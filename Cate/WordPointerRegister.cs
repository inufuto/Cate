using System;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate
{
    public abstract class WordPointerRegister : PointerRegister
    {
        public override WordRegister WordRegister { get; }
        public ByteRegister? Low => WordRegister?.Low ?? null;
        public ByteRegister? High => WordRegister?.High ?? null;

        public override string AsmName
        {
            get
            {
                Debug.Assert(WordRegister != null, nameof(WordRegister) + " != null");
                return WordRegister.AsmName;
            }
        }

        protected WordPointerRegister(int byteCount, WordRegister wordRegister) : base(wordRegister.Id, byteCount, wordRegister.Name)
        {
            WordRegister = wordRegister;
        }

        public override bool Conflicts(Register? register)
        {
            if (register is WordPointerRegister wordPointerRegister && WordRegister.Conflicts(wordPointerRegister.WordRegister)) {
                return true;
            }
            if (WordRegister.Conflicts(register)) {
                return true;
            }
            return base.Conflicts(register);
        }

        public override bool Contains(ByteRegister byteRegister)
        {
            return WordRegister.Contains(byteRegister);
        }

        public override bool Matches(Register register)
        {
            if (WordRegister.Matches(register)) {
                return true;
            }
            return base.Matches(register);
        }


        public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            WordRegister.Save(writer, comment, instruction, tabCount);
        }

        public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            WordRegister.Restore(writer, comment, instruction, tabCount);
        }

        public override void Save(Instruction instruction)
        {
            WordRegister.Save(instruction);
        }

        public override void Restore(Instruction instruction)
        {
            WordRegister.Restore(instruction);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            WordRegister.LoadConstant(instruction, value);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            WordRegister.LoadFromMemory(instruction, label);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            WordRegister.StoreToMemory(instruction, label);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            var variableRegister = instruction.GetVariableRegister(variable, offset);
            if (variableRegister is WordRegister wordRegister) {
                WordRegister.CopyFrom(instruction, wordRegister);
                instruction.SetVariableRegister(variable, offset, this);
                return;
            }
            if (variableRegister is PointerRegister pointerRegister) {
                CopyFrom(instruction, pointerRegister);
                instruction.SetVariableRegister(variable, offset, this);
                return;
            }
            base.LoadFromMemory(instruction, variable, offset);
        }

        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            WordRegister.LoadIndirect(instruction, pointerRegister, offset);
        }

        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            WordRegister.StoreIndirect(instruction, pointerRegister, offset);
        }


        public override void CopyFrom(Instruction instruction, PointerRegister sourceRegister)
        {
            if (sourceRegister.WordRegister != null) {
                WordRegister.CopyFrom(instruction, sourceRegister.WordRegister);
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            throw new NotImplementedException();
        }
    }
}
