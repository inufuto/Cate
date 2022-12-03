using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Mos6502
{
    internal abstract class ByteRegister : Cate.ByteRegister
    {
        public static readonly ByteRegister A = new Accumulator(1, "a");
        public static readonly ByteRegister X = new IndexRegister(2, "x");
        public static readonly ByteRegister Y = new IndexRegister(3, "y");

        public static readonly List<Cate.ByteRegister> Registers = new List<Cate.ByteRegister>() { A, X, Y };

        public static Cate.ByteRegister FromId(int id)
        {
            return Registers.First(r => r.Id == id);
        }

        protected ByteRegister(int id, string name) : base(id, name) { }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tld" + Name + "\t#" + value);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tld" + Name + "\t" + variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
            instruction.ChangedRegisters.Add(this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tst" + Name + "\t" + variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tld" + Name + "\t" + label);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tst" + Name + "\t" + label);
        }

        public override void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
        {
            Debug.Assert(Equals(A));
            Debug.Assert(pointerRegister is WordZeroPage);
            if (pointerRegister.IsOffsetInRange(offset)) {
                Debug.Assert(offset >= 0 && offset < 0x100);
                ByteOperation.UsingRegister(instruction, Y, () =>
                {
                    Y.LoadConstant(instruction, offset);
                    instruction.WriteLine("\tld" + Name + "\t(" + pointerRegister.Name + "),y");
                });
                instruction.ChangedRegisters.Add(this);
                instruction.RemoveRegisterAssignment(this);
                instruction.ResultFlags |= Instruction.Flag.Z;
            }
            else {
                WordOperation.UsingAnyRegister(instruction, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, pointerRegister);
                    temporaryRegister.Add(instruction, offset);
                    LoadIndirect(instruction, temporaryRegister, 0);
                    instruction.RemoveRegisterAssignment(temporaryRegister);
                    instruction.ChangedRegisters.Add(temporaryRegister);
                });
            }
        }

        public override void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
        {
            Debug.Assert(Equals(A));
            Debug.Assert(pointerRegister is WordZeroPage);
            if (pointerRegister.IsOffsetInRange(offset)) {
                Y.LoadConstant(instruction, offset);
                instruction.WriteLine("\tst" + Name + "\t(" + pointerRegister.Name + "),y");
            }
            else {
                pointerRegister.Add(instruction, offset);
                StoreIndirect(instruction, pointerRegister, 0);
                instruction.RemoveRegisterAssignment(pointerRegister);
                instruction.ChangedRegisters.Add(pointerRegister);
            }
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister register)
        {
            if (register.Equals(this)) {
                return;
            }
            switch (register) {
                case ByteRegister byteRegister:
                    instruction.WriteLine("\tt" + byteRegister + this);
                    instruction.ChangedRegisters.Add(this);
                    instruction.RemoveRegisterAssignment(this);
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    return;
                case ByteZeroPage zeroPage:
                    LoadFromMemory(instruction, zeroPage.Name);
                    return;
            }
            throw new NotImplementedException();
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + Name);
            }
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }



        //public override void Operate(Instruction instruction, string operation, bool change, string operand)
        //{
        //    instruction.WriteLine("\t" + operation + Name + "\t" + operand);
        //    if (!change)
        //        return;
        //    instruction.ChangedRegisters.Add(this);
        //    instruction.RemoveVariableRegister(this);
        //}

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (operand is VariableOperand variableOperand) {
                var register = instruction.GetVariableRegister(variableOperand);
                switch (register) {
                    case ByteZeroPage zeroPage:
                        instruction.WriteLine("\t" + operation + "\t" + zeroPage);
                        instruction.ResultFlags |= Instruction.Flag.Z;
                        if (!change)
                            return;
                        instruction.ChangedRegisters.Add(this);
                        instruction.RemoveRegisterAssignment(this);
                        return;
                    case ByteRegister byteRegister:
                        Cate.Compiler.Instance.ByteOperation.UsingAnyRegister(instruction, ByteZeroPage.Registers,
                            temporary =>
                            {
                                temporary.CopyFrom(instruction, byteRegister);
                                instruction.WriteLine("\t" + operation + "\t" + temporary);
                            });
                        instruction.ResultFlags |= Instruction.Flag.Z;
                        if (!change)
                            return;
                        instruction.ChangedRegisters.Add(this);
                        instruction.RemoveRegisterAssignment(this);
                        return;
                }
            }
            Cate.Compiler.Instance.ByteOperation.Operate(instruction, operation, change, operand);
        }

        public abstract void Decrement(Instruction instruction);
    }

    internal class Accumulator : ByteRegister
    {
        public Accumulator(int id, string name) : base(id, name) { }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            instruction.WriteLine("\t" + operation + "\t" + operand);
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }


        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpha");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpla");
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            writer.WriteLine("\tpha" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            writer.WriteLine("\tpla" + comment);
        }

        public override void Decrement(Instruction instruction)
        {
            instruction.WriteLine("\tsec|sbc\t#1");
        }
    }

    internal class IndexRegister : ByteRegister
    {
        public IndexRegister(int id, string name) : base(id, name) { }

        public override void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
        {
            ByteOperation.UsingRegister(instruction, A, () =>
            {
                A.LoadIndirect(instruction, pointerRegister, offset);
                CopyFrom(instruction, A);
            });
        }

        public override void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
        {
            ByteOperation.UsingRegister(instruction, A, () =>
            {
                A.CopyFrom(instruction, this);
                A.StoreIndirect(instruction, pointerRegister, offset);
            });
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister register)
        {
            if (register is IndexRegister) {
                ByteOperation.UsingRegister(instruction, A, () => { });
                A.CopyFrom(instruction, register);
                base.CopyFrom(instruction, A);
                return;
            }
            base.CopyFrom(instruction, register);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (operation.StartsWith("cp")) {
                Debug.Assert(!change);
                if (operand is ConstantOperand || operand is VariableOperand) {
                    base.Operate(instruction, operation, change, operand);
                    return;
                }
                operation = "cmp";
            }
            ByteOperation.UsingRegister(instruction, A, () =>
            {
                A.CopyFrom(instruction, this);
                A.Operate(instruction, operation, change, operand);
                if (change) {
                    CopyFrom(instruction, A);
                }
            });
        }

        public override void Decrement(Instruction instruction)
        {
            instruction.WriteLine("\tde" + Name);
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            throw new NotImplementedException();
        }

        public override void Restore(Instruction instruction)
        {
            throw new NotImplementedException();
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            // cannot save : don't assign to variable
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            // cannot save : don't assign to variable
        }
    }

}