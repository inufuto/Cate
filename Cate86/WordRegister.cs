using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Inu.Cate.I8086
{
    enum SegmentRegister
    {
        Es, Cs, Ss, Ds
    }

    internal abstract class WordRegister : Cate.WordRegister
    {
        public static List<Cate.WordRegister> Registers = new List<Cate.WordRegister>();

        public static WordRegister Ax = new PairRegister(11, "ax", ByteRegister.Ah, ByteRegister.Al);
        public static WordRegister Dx = new PairRegister(13, "dx", ByteRegister.Dh, ByteRegister.Dl);
        public static WordRegister Cx = new PairRegister(12, "cx", ByteRegister.Ch, ByteRegister.Cl);
        public static WordRegister
            Bx = new PairRegister(14, "bx", ByteRegister.Bh, ByteRegister.Bl, SegmentRegister.Ds);
        public static WordRegister Bp = new IndexRegister(16, "bp", SegmentRegister.Ss);
        public static WordRegister Si = new IndexRegister(17, "si");
        public static WordRegister Di = new IndexRegister(18, "di");

        public static List<Cate.WordRegister> PointerRegisters => Registers.Where(r => r.IsPointer(0)).ToList();
        public abstract IEnumerable<Register> ByteRegisters { get; }

        public static List<Cate.WordRegister> PointerOrder = new List<Cate.WordRegister>() { Bx, Si, Di, Bp, Ax, Dx, Cx };

        public static PairRegister? FromName(string name)
        {
            foreach (var register in Registers) {
                if (register is PairRegister pairRegister && pairRegister.Name.Equals(name)) return pairRegister;
            }
            return null;
        }

        public static string AsPointer(Cate.WordRegister wordRegister)
        {
            var defaultSegmentRegister = ((WordRegister)wordRegister).DefaultSegmentRegister;
            return defaultSegmentRegister == SegmentRegister.Ds
                ? wordRegister.ToString()
                : "ds:" + wordRegister;
        }


        public readonly SegmentRegister? DefaultSegmentRegister;

        protected WordRegister(int id, string name, SegmentRegister? defaultSegmentRegister = null) : base(id, name)
        {
            DefaultSegmentRegister = defaultSegmentRegister;
            Registers.Add(this);
        }

        public override bool IsAddable() => true;
        public override bool IsIndex() => DefaultSegmentRegister != null;

        public override void Add(Instruction instruction, int offset)
        {
            switch (offset) {
                case 0:
                    return;
                case 1:
                    instruction.WriteLine("\tinc " + this);
                    return;
                case -1:
                    instruction.WriteLine("\tdec " + this);
                    return;
            }
            if (offset > 0) {
                instruction.WriteLine("\tadd " + this + "," + offset);
                return;
            }
            instruction.WriteLine("\tsub " + this + "," + (-offset));
        }
        public override bool IsOffsetInRange(int offset) => true;
        private bool IsPointer()
        {
            return DefaultSegmentRegister != null;
        }

        public override bool IsPointer(int offset)
        {
            return IsPointer();
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tmov " + this + "," + value);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov " + this + "," + label);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            var variableRegister = instruction.GetVariableRegister(variable, offset);
            if (variableRegister is WordRegister wordRegister) {
                CopyFrom(instruction, wordRegister);
                return;
            }
            string label = variable.MemoryAddress(offset);
            instruction.WriteLine("\tmov " + this + ",[" + label + "]");
            instruction.SetVariableRegister(variable, offset, this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov [" + label + "]," + this);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            if (!pointerRegister.IsPointer(offset)) {
                WordOperation.UsingAnyRegister(instruction, PointerRegisters, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, pointerRegister);
                    LoadIndirect(instruction, temporaryRegister, offset);
                });
                return;
            }
            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
            instruction.WriteLine("\tmov " + this + ",[" + AsPointer(pointerRegister) + addition + "]");
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            if (!pointerRegister.IsPointer(offset)) {
                WordOperation.UsingAnyRegister(instruction, PointerRegisters, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, pointerRegister);
                    StoreIndirect(instruction, temporaryRegister, offset);
                });
                return;
            }
            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
            instruction.WriteLine("\tmov [" + AsPointer(pointerRegister) + addition + "]," + this);
        }


        public override void Load(Instruction instruction, Operand sourceOperand)
        {
            switch (sourceOperand) {
                case IntegerOperand sourceIntegerOperand:
                    var value = sourceIntegerOperand.IntegerValue;
                    LoadConstant(instruction, value);
                    return;
                case PointerOperand sourcePointerOperand:
                    instruction.WriteLine("\tmov " + this + "," + sourcePointerOperand.MemoryAddress());
                    instruction.RemoveRegisterAssignment(this);
                    instruction.ChangedRegisters.Add(this);
                    return;
                case VariableOperand sourceVariableOperand: {
                        var sourceVariable = sourceVariableOperand.Variable;
                        var sourceOffset = sourceVariableOperand.Offset;
                        if (sourceVariable.Register is WordRegister sourceRegister) {
                            Debug.Assert(sourceOffset == 0);
                            if (!Equals(sourceRegister, this)) {
                                CopyFrom(instruction, sourceRegister);
                            }
                            return;
                        }
                        LoadFromMemory(instruction, sourceVariable, sourceOffset);
                        return;
                    }
                case IndirectOperand sourceIndirectOperand: {
                        var pointer = sourceIndirectOperand.Variable;
                        var offset = sourceIndirectOperand.Offset;
                        {
                            if (pointer.Register is WordRegister pointerRegister) {
                                LoadIndirect(instruction, pointerRegister, offset);
                                return;
                            }
                        }
                        WordOperation.UsingAnyRegister(instruction, PointerRegisters, pointerRegister =>
                        {
                            pointerRegister.LoadFromMemory(instruction, pointer, 0);
                            LoadIndirect(instruction, pointerRegister, offset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        private void Store(Instruction instruction, Variable variable, int offset)
        {
            var destinationAddress = variable.MemoryAddress(offset);
            instruction.WriteLine("\tmov [" + destinationAddress + "]," + this);
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void Store(Instruction instruction, AssignableOperand destinationOperand)
        {
            switch (destinationOperand) {
                case VariableOperand destinationVariableOperand: {
                        var destinationVariable = destinationVariableOperand.Variable;
                        var destinationOffset = destinationVariableOperand.Offset;
                        if (destinationVariable.Register is WordRegister destinationRegister) {
                            Debug.Assert(destinationOffset == 0);
                            if (!Equals(destinationRegister, this)) {
                                destinationRegister.CopyFrom(instruction, this);
                            }
                            instruction.SetVariableRegister(destinationVariable, destinationOffset, destinationRegister);
                            return;
                        }
                        Store(instruction, destinationVariable, destinationOffset);
                        return;
                    }
                case IndirectOperand destinationIndirectOperand: {
                        var destinationPointer = destinationIndirectOperand.Variable;
                        var destinationOffset = destinationIndirectOperand.Offset;
                        if (destinationPointer.Register is WordRegister destinationPointerRegister) {
                            StoreIndirect(instruction,
                                destinationPointerRegister, destinationOffset);
                            return;
                        }
                        WordOperation.UsingAnyRegister(instruction, PointerRegisters, pointerRegister =>
                        {
                            pointerRegister.LoadFromMemory(instruction, destinationPointer, 0);
                            StoreIndirect(instruction, pointerRegister, destinationOffset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }




        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            if (Equals(this, sourceRegister)) return;
            instruction.WriteLine("\tmov " + this + "," + sourceRegister);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (operand is ConstantOperand constantOperand) {
                instruction.WriteLine("\t" + operation + this + "," + constantOperand.MemoryAddress());
                return;
            }
            if (operand is VariableOperand variableOperand) {
                var sourceVariable = variableOperand.Variable;
                var sourceOffset = variableOperand.Offset;
                if (sourceVariable.Register is WordRegister sourceRegister) {
                    Debug.Assert(sourceOffset == 0);
                    instruction.WriteLine("\t" + operation + this + "," + sourceRegister);
                    return;
                }
                instruction.WriteLine("\t" + operation + this + ",[" + variableOperand.MemoryAddress() + "]");
                return;
            }
            if (operand is IndirectOperand indirectOperand) {
                var pointer = indirectOperand.Variable;
                var offset = indirectOperand.Offset;
                if (pointer.Register is WordRegister pointerRegister) {
                    var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
                    instruction.WriteLine("\t" + operation + this + ",[" + AsPointer(pointerRegister) + addition + "]");
                    return;
                }
                WordOperation.UsingAnyRegister(instruction, PointerRegisters, temporaryRegister =>
                {
                    temporaryRegister.Load(instruction, operand);
                    instruction.WriteLine("\t" + operation + this + "," + temporaryRegister);
                });
                return;
            }
            throw new NotImplementedException();
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpush " + this + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpop " + this + comment);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpush " + this);
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpop " + this);
        }
    }

    internal class PairRegister : WordRegister
    {
        public readonly ByteRegister HighByteRegister;
        public readonly ByteRegister LowByteRegister;

        public PairRegister(int id, string name, ByteRegister highByteRegister, ByteRegister lowByteRegister,
            SegmentRegister? defaultSegmentRegister = null) : base(id, name, defaultSegmentRegister)
        {
            HighByteRegister = highByteRegister;
            LowByteRegister = lowByteRegister;
        }

        public override bool IsPair() => true;

        public override Cate.ByteRegister? High => HighByteRegister;
        public override Cate.ByteRegister? Low => LowByteRegister;

        public override IEnumerable<Register> ByteRegisters => new[] { HighByteRegister, LowByteRegister };

    }

    internal class IndexRegister : WordRegister
    {
        public IndexRegister(int id, string name, SegmentRegister? defaultSegmentRegister = SegmentRegister.Ds) : base(id, name, defaultSegmentRegister)
        { }

        public override bool IsPair() => false;
        public override IEnumerable<Register> ByteRegisters => Array.Empty<ByteRegister>();
    }
}
