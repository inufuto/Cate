using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public override void Exchange(Instruction instruction, Cate.ByteRegister register)
        {
            var candidates = ByteRegister.Registers.Where(r => !Equals(r, this) && !Equals(r, register)).ToList();
            ByteOperation.UsingAnyRegister(instruction, candidates, temporaryRegister =>
            {
                temporaryRegister.CopyFrom(instruction, this);
                CopyFrom(instruction, register);
                register.CopyFrom(instruction, temporaryRegister);
            });
        }

        private const int MinId = 100;

        private static List<Cate.ByteRegister>? registers;
        public readonly WordRegister WordRegister;

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
            this.WordRegister = wordRegister;
        }
        public int Index => WordRegister.Index;

        public override bool Conflicts(Register? register)
        {
            if (register is WordRegister otherWordRegister) {
                return otherWordRegister.Index == Index;
            }
            return base.Conflicts(register);
        }

        public override bool Matches(Register register)
        {
            if (register is WordRegister wordRegister && Equals(wordRegister.ByteRegister, this)) return true;
            return base.Matches(register);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            WordRegister.Save(writer, comment, jump, tabCount);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            WordRegister.Restore(writer, comment, jump, tabCount);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            WordRegister.LoadConstant(instruction, ByteConst(value));
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public static string ByteConst(string value) => value + " shl 8";
        public static string ByteConst(int value) => ByteConst(value.ToString());

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
                if (pointerRegister.Conflicts(this)) {
                    instruction.WriteLine("\tmovb\t*" + pointerRegister.Name + "," + Name);
                    instruction.WriteLine("\tandi\t" + Name + ",>ff00");
                }
                else {
                    Clear(instruction);
                    instruction.WriteLine("\tmovb\t*" + pointerRegister.Name + "," + Name);
                }
            }
            else {
                void ForRegister(Cate.WordRegister register1)
                {
                    Clear(instruction);
                    instruction.WriteLine("\tmovb\t@" + offset + "(" + register1.Name + ")," + Name);
                }

                if (pointerRegister.IsOffsetInRange(offset) && !pointerRegister.Conflicts(this)) {
                    ForRegister(pointerRegister);
                }
                else {
                    var candidates = WordOperation.PointerRegisters(offset).Where(r => !r.Conflicts(this)).ToList();
                    WordOperation.UsingAnyRegister(instruction, candidates,
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
            Clear(instruction);
            instruction.WriteLine("\tmovb\t@" + label + "," + Name);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmovb\t" + Name + ",@" + label);
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            instruction.WriteLine("\tmov\t" + sourceRegister.Name + "," + Name);
            instruction.RemoveRegisterAssignment(WordRegister);
            instruction.ChangedRegisters.Add(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + Name);
            }
            if (change) {
                instruction.RemoveRegisterAssignment(this);
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
            WordRegister.Save(instruction);
        }

        public override void Restore(Instruction instruction)
        {
            WordRegister.Restore(instruction);
        }

        public WordRegister Expand(Instruction instruction, bool signed)
        {
            var operation = signed ? "sra" : "srl";
            instruction.WriteLine("\t" + operation + "\t" + Name + ",8");
            return WordRegister;
        }

        public void Clear(Instruction instruction)
        {
            WordRegister.Clear(instruction);
        }
    }
}
