using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.I8086
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public static List<Cate.ByteRegister> Registers = new List<Cate.ByteRegister>();

        public static ByteRegister Al = new ByteRegister(1, "al");
        public static ByteRegister Ah = new ByteRegister(5, "ah");
        public static ByteRegister Dl = new ByteRegister(3, "dl");
        public static ByteRegister Dh = new ByteRegister(7, "dh");
        public static ByteRegister Cl = new ByteRegister(2, "cl");
        public static ByteRegister Ch = new ByteRegister(6, "ch");
        public static ByteRegister Bl = new ByteRegister(4, "bl");
        public static ByteRegister Bh = new ByteRegister(8, "bh");

        //public static IEnumerable<ByteRegister> AllocatableRegisters = new[] { Al, Cl, Dl, Bl, Ah, Ch, Dh, Bh };

        public ByteRegister(int id, string name) : base(id, name)
        {
            Registers.Add(this);
        }

        public override bool Conflicts(Register? register)
        {
            switch (register) {
                case WordRegister wordRegister:
                    if (wordRegister.Contains(this))
                        return true;
                    break;
                case ByteRegister byteRegister:
                    if (PairRegister != null && PairRegister.Contains(byteRegister))
                        return true;
                    break;
            }
            return base.Conflicts(register);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(PairRegister != null);
            PairRegister.Save(writer, comment, jump, tabCount);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(PairRegister != null);
            PairRegister.Restore(writer, comment, jump, tabCount);
        }

        public override void Save(Instruction instruction)
        {
            Debug.Assert(PairRegister != null);
            PairRegister.Save(instruction);
        }

        public override void Restore(Instruction instruction)
        {
            Debug.Assert(PairRegister != null);
            PairRegister.Restore(instruction);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tmov " + this + "," + value);
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            if (value == 0) {
                instruction.WriteLine("\txor " + this + "," + this);
            }
            else {
                base.LoadConstant(instruction, value);
            }
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov " + this + ",[" + label + "]");
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov [" + label + "]," + this);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            LoadFromMemory(instruction, variable.MemoryAddress(offset));
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            StoreToMemory(instruction, variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsPointer(offset));
            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
            instruction.WriteLine("\tmov " + this + ",[" + WordRegister.AsPointer(pointerRegister) + addition + "]");
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsPointer(offset));
            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
            instruction.WriteLine("\tmov [" + WordRegister.AsPointer(pointerRegister) + addition + "]," + this);
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            instruction.WriteLine("\tmov " + this + "," + sourceRegister);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + this);
            }
            //instruction.ChangedRegisters.Add(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    Operate(instruction, operation, change, integerOperand.IntegerValue.ToString());
                    return;
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        var register = instruction.GetVariableRegister(variableOperand);
                        if (register is ByteRegister byteRegister) {
                            Debug.Assert(offset == 0);
                            Operate(instruction, operation, change, register.ToString());
                            return;
                        }
                        Operate(instruction, operation, change, "[" + variable.MemoryAddress(offset) + "]");
                        return;
                    }
                case IndirectOperand indirectOperand: {
                        void ForRegister(Cate.WordRegister register)
                        {
                            var offset = indirectOperand.Offset;
                            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
                            Operate(instruction, operation, change, "[" + WordRegister.AsPointer(register) + addition + "]");
                        }


                        var pointer = indirectOperand.Variable;
                        {
                            var register = instruction.GetVariableRegister(pointer, 0);
                            if (register is WordRegister pointerRegister) {
                                ForRegister(pointerRegister);
                                return;
                            }
                            WordOperation.UsingAnyRegister(instruction, WordRegister.PointerRegisters,
                                temporaryRegister =>
                            {
                                temporaryRegister.LoadFromMemory(instruction, indirectOperand.Variable, 0);
                                instruction.SetVariableRegister(indirectOperand.Variable, 0, temporaryRegister);
                                ForRegister(temporaryRegister);
                            });
                            return;
                        }
                    }
            }
            throw new NotImplementedException();
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            instruction.WriteLine("\t" + operation + this + "," + operand);
            if (change) {
                instruction.ChangedRegisters.Add(this);
                instruction.RemoveRegisterAssignment(this);
            }
        }

        public override Cate.WordRegister? PairRegister => I8086.PairRegister.FromName(Name.ToCharArray()[0] + "x");
    }
}
