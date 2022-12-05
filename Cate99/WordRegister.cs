using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Inu.Cate.Tms99
{
    internal class WordRegister : Cate.WordRegister
    {
        private const int MinId = 1;
        private const int Count = 10;

        private static List<Cate.WordRegister>? registers;
        public readonly int Index;

        private WordRegister(int index) : base(MinId + index, NameFromIndex(index))
        {
            Index = index;
        }

        public static List<Cate.WordRegister> Registers
        {
            get
            {
                if (registers != null) return registers;
                registers = new List<Cate.WordRegister>();
                for (var index = 0; index < Count; ++index) {
                    registers.Add(new WordRegister(index));
                }
                return registers;
            }
        }

        public static List<Cate.WordRegister> StructurePointers => Registers.Where(r => ((WordRegister)r).Index != 0).ToList();

        public ByteRegister ByteRegister => ByteRegister.FromIndex(Index);

        private static string NameFromIndex(int index) => "r" + index;
        public static WordRegister FromIndex(int index)
        {
            var register = Registers.Find(r => ((WordRegister)r).Index == index);
            Debug.Assert(register != null);
            return (WordRegister)register;
        }

        public override bool Conflicts(Register? register)
        {
            if (register is ByteRegister byteRegister) {
                return byteRegister.Index == Index;
            }
            return base.Conflicts(register);
        }

        public override bool Matches(Register register)
        {
            if (register is ByteRegister byteRegister && Equals(byteRegister, ByteRegister)) return true;
            return base.Matches(register);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            Save(writer, Index, comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            void WriteCode()
            {
                Instruction.WriteTabs(writer, tabCount);
                WordRegister.Restore(writer, Index, comment);
            }

            if (jump) {
                SaveStatus(writer, tabCount);
                WriteCode();
                RestoreStatus(writer, tabCount);
                return;
            }
            WriteCode();
        }

        private void SaveStatus(StreamWriter writer, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tstst\tr15");
            writer.WriteLine("\tstwp\tr13");
        }

        private static int statusSavingCount = 0;

        private void RestoreStatus(StreamWriter writer, int tabCount)
        {
            var label = "_st_" + statusSavingCount;
            ++statusSavingCount;
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tli\tr14," + label);
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\trtwp");
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\t" + label + ":");
        }


        public static void Save(StreamWriter writer, int index, string? comment)
        {
            writer.WriteLine("\tdect r10 | mov " + NameFromIndex(index) + ",*r10" + comment);
        }

        public static void Restore(StreamWriter writer, int index, string? comment)
        {
            writer.WriteLine("\tmov *r10+," + NameFromIndex(index) + comment);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tdect r10 | mov " + NameFromIndex(Index) + ",*r10");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tmov *r10+," + NameFromIndex(Index));
        }

        public override void Add(Instruction instruction, int offset)
        {
            instruction.WriteLine("\tai\t" + Name + "," + offset);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override bool IsOffsetInRange(int offset) => Index != 0 || offset == 0;

        public override bool IsPointer(int offset) => IsOffsetInRange(offset);

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tli\t" + Name + "," + value);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            if (value == 0) {
                Clear(instruction);
                instruction.ChangedRegisters.Add(this);
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            base.LoadConstant(instruction, value);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov\t@" + label + "," + Name);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov\t" + Name + ",@" + label);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            LoadFromMemory(instruction, variable.MemoryAddress(offset));
        }


        public override void Load(Instruction instruction, Operand sourceOperand)
        {
            switch (sourceOperand) {
                case IntegerOperand sourceIntegerOperand:
                    LoadConstant(instruction, sourceIntegerOperand.IntegerValue);
                    return;
                case PointerOperand sourcePointerOperand:
                    LoadConstant(instruction, sourcePointerOperand.MemoryAddress());
                    return;
                case VariableOperand sourceVariableOperand: {
                        var sourceVariable = sourceVariableOperand.Variable;
                        var sourceOffset = sourceVariableOperand.Offset;
                        var variableRegister = instruction.GetVariableRegister(sourceVariable, sourceOffset);
                        if (variableRegister is WordRegister sourceRegister) {
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
                        if (pointer.Register is WordRegister pointerRegister) {
                            LoadIndirect(instruction, pointerRegister, offset);
                            return;
                        }
                        WordOperation.UsingAnyRegister(instruction, WordOperation.PointerRegisters(offset), temporaryRegister =>
                        {
                            temporaryRegister.LoadFromMemory(instruction, pointer, 0);
                            LoadIndirect(instruction, temporaryRegister, offset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
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
                            instruction.RemoveRegisterAssignment(this);
                            return;
                        }
                        StoreToMemory(instruction, destinationVariable.MemoryAddress(destinationOffset));
                        instruction.SetVariableRegister(destinationVariable, destinationOffset, this);
                        return;
                    }
                case IndirectOperand destinationIndirectOperand: {
                        var pointer = destinationIndirectOperand.Variable;
                        var offset = destinationIndirectOperand.Offset;
                        if (pointer.Register is WordRegister destinationPointerRegister) {
                            StoreIndirect(instruction,
                                destinationPointerRegister, offset);
                            return;
                        }
                        WordOperation.UsingAnyRegister(instruction, WordOperation.PointerRegisters(offset),
                            temporaryRegister =>
                        {
                            temporaryRegister.LoadFromMemory(instruction, pointer, 0);
                            StoreIndirect(instruction, temporaryRegister, offset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            if (offset == 0) {
                instruction.WriteLine("\tmov\t*" + pointerRegister.Name + "," + Name);
                instruction.ChangedRegisters.Add(this);
            }
            else {
                void ForRegister(Register wordRegister)
                {
                    instruction.WriteLine("\tmov\t@" + offset + "(" + wordRegister.Name + ")," + Name);
                    instruction.ChangedRegisters.Add(this);
                }

                if (pointerRegister.IsOffsetInRange(offset)) {
                    ForRegister(pointerRegister);
                }
                else {
                    WordOperation.UsingAnyRegister(instruction, WordOperation.PointerRegisters(offset),
                        temporaryRegister =>
                    {
                        temporaryRegister.CopyFrom(instruction, pointerRegister);
                        ForRegister(temporaryRegister);
                    });
                }
            }
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            if (offset == 0) {
                instruction.WriteLine("\tmov\t" + Name + ",*" + pointerRegister);
            }
            else {
                void ForRegister(Cate.WordRegister wordRegister)
                {
                    instruction.WriteLine("\tmov\t" + Name + ",@" + offset + "(" + wordRegister.Name + ")");
                }

                if (pointerRegister.IsOffsetInRange(offset)) {
                    ForRegister(pointerRegister);
                }
                else {
                    WordOperation.UsingAnyRegister(instruction, WordOperation.PointerRegisters(offset),
                        temporaryRegister =>
                        {
                            temporaryRegister.CopyFrom(instruction, pointerRegister);
                            ForRegister(temporaryRegister);
                        });
                }
            }
        }

        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            instruction.WriteLine("\tmov\t" + sourceRegister.Name + "," + Name);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new NotImplementedException();
        }

        public void Clear(Instruction instruction)
        {
            instruction.WriteLine("\tclr\t" + Name);
        }
    }
}
