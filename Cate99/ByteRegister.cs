using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Tms99
{
    internal class ByteRegister : Cate.ByteRegister
    {
        private const int MinId = 100;

        private static List<Cate.ByteRegister>? registers;
        private readonly WordRegister wordRegister;

        public static List<Cate.ByteRegister> Registers
        {
            get
            {
                if (registers != null) return registers;
                registers = new List<Cate.ByteRegister>();
                foreach (var wordRegister in WordRegister.Registers) {
                    registers.Add(new ByteRegister((WordRegister)wordRegister));
                }
                return registers;
            }
        }

        public static ByteRegister FromIndex(int index)
        {
            var register = Registers.Find(r => ((ByteRegister)r).Index == index);
            Debug.Assert(register != null);
            return (ByteRegister)register;
        }


        private ByteRegister(WordRegister wordRegister) : base(MinId + wordRegister.Index, wordRegister.Name)
        {
            this.wordRegister = wordRegister;
        }
        public int Index => wordRegister.Index;
        public WordRegister WordRegister => WordRegister.FromIndex(Index);

        public override bool Conflicts(Register? register)
        {
            if (register is WordRegister otherWordRegister) {
                return otherWordRegister.Index == Index;
            }
            return base.Conflicts(register);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            wordRegister.Save(writer, comment, jump, tabCount);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            wordRegister.Restore(writer, comment, jump, tabCount);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            wordRegister.LoadConstant(instruction, value);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveVariableRegister(this);
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            if (value == 0) {
                instruction.WriteLine("\tclr\t" + Name);
                instruction.ChangedRegisters.Add(this);
                instruction.RemoveVariableRegister(this);
                return;
            }
            base.LoadConstant(instruction, value);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            LoadFromMemory(instruction, variable.MemoryAddress(offset));
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            StoreToMemory(instruction, variable.MemoryAddress(offset));
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            if (offset == 0) {
                instruction.WriteLine("\tmovb\t*" + pointerRegister.Name + "," + Name);
            }
            else {
                void ForRegister(Cate.WordRegister register1)
                {
                    instruction.WriteLine("\tmovb\t@" + offset + "(" + register1.Name + ")," + Name);
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
            instruction.WriteLine("\tsrl\t" + Name + ",8");
            instruction.RemoveVariableRegister(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            instruction.WriteLine("\tswpb\t" + Name);
            if (offset == 0) {
                instruction.WriteLine("\tmovb\t" + Name + ",*" + pointerRegister);
            }
            else {
                void ForRegister(Cate.WordRegister register)
                {
                    instruction.WriteLine("\tmovb\t" + Name + ",@" + offset + "(" + register.Name + ")");
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

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmovb\t@" + label + "," + Name);
            instruction.WriteLine("\tsrl\t" + Name + ",8");
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveVariableRegister(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tswpb\t" + Name);
            instruction.WriteLine("\tmov\t" + Name + ",@" + label);
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            if (sourceRegister is ByteRegister sourceByteRegister) {
                wordRegister.CopyFrom(instruction, sourceByteRegister.wordRegister);
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + Name);
            }
            if (change) {
                instruction.RemoveVariableRegister(this);
                instruction.ChangedRegisters.Add(this);
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new NotImplementedException();
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            wordRegister.Save(instruction);
        }

        public override void Restore(Instruction instruction)
        {
            wordRegister.Restore(instruction);
        }
    }
}
