using System.Diagnostics;
using System.IO;

namespace Inu.Cate
{
    public abstract class WordPointerRegister : PointerRegister
    {
        public override WordRegister? WordRegister { get; }
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

        protected WordPointerRegister(WordRegister wordRegister) : base(wordRegister.Id, wordRegister.Name)
        {
            WordRegister = wordRegister;
        }

        public override bool Conflicts(Register? register)
        {
            if (WordRegister != null) {
                if (register is WordPointerRegister wordPointerRegister && WordRegister.Conflicts(wordPointerRegister.WordRegister)) {
                    return true;
                }
                if (WordRegister.Conflicts(register)) {
                    return true;
                }
            }
            return base.Conflicts(register);
        }

        public override bool Contains(ByteRegister byteRegister)
        {
            return WordRegister != null && WordRegister.Contains(byteRegister);
        }

        public override bool Matches(Register register)
        {
            if (WordRegister != null && WordRegister.Matches(register)) {
                return true;
            }
            return base.Matches(register);
        }


        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            WordRegister?.Save(writer, comment, jump, tabCount);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            WordRegister?.Restore(writer, comment, jump, tabCount);
        }

        public override void Save(Instruction instruction)
        {
            WordRegister?.Save(instruction);
        }

        public override void Restore(Instruction instruction)
        {
            WordRegister?.Restore(instruction);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            WordRegister?.LoadConstant(instruction, value);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            WordRegister?.LoadFromMemory(instruction, label);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            WordRegister?.StoreToMemory(instruction, label);
        }

        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            WordRegister?.LoadIndirect(instruction, pointerRegister, offset);
        }

        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            WordRegister?.StoreIndirect(instruction, pointerRegister, offset);
        }


        public override void CopyFrom(Instruction instruction, PointerRegister sourceRegister)
        {
            if (WordRegister != null && sourceRegister.WordRegister != null) {
                WordRegister.CopyFrom(instruction, sourceRegister.WordRegister);
            }
        }
    }
}
