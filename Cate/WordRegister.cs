using System;

namespace Inu.Cate
{
    public abstract class WordRegister : Register
    {
        protected WordRegister(int id, string name) : base(id, 2, name) { }
        public virtual ByteRegister? Low => null;
        public virtual ByteRegister? High => null;
        public virtual bool IsPair() => Low != null && High != null;

        public virtual bool Contains(ByteRegister byteRegister)
        {
            return Equals(Low, byteRegister) || Equals(High, byteRegister);
        }
        public override bool Conflicts(Register? register)
        {
            return base.Conflicts(register) || register is ByteRegister byteRegister && Contains(byteRegister);
        }

        public override bool Matches(Register register)
        {
            return base.Matches(register) || register is ByteRegister byteRegister && Contains(byteRegister);
        }

        public abstract void Add(Instruction instruction, int offset);
        public virtual bool IsAddable() => false;
        public virtual bool IsIndex() => false;
        public abstract bool IsOffsetInRange(int offset);
        public abstract bool IsPointer(int offset);

        public abstract void LoadConstant(Instruction instruction, string value);
        public void LoadConstant(Instruction instruction, int value)
        {
            LoadConstant(instruction, value.ToString());
        }
        public abstract void LoadFromMemory(Instruction instruction, string label);
        public abstract void StoreToMemory(Instruction instruction, string label);
        public abstract void Load(Instruction instruction, Operand operand);
        public abstract void Store(Instruction instruction, AssignableOperand operand);
        public abstract void LoadFromMemory(Instruction instruction, Variable variable, int offset);


        public abstract void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset);
        public abstract void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset);

        public abstract void CopyFrom(Instruction instruction, WordRegister register);


        public abstract void Operate(Instruction instruction, string operation, bool change, Operand operand);
        public abstract void Save(Instruction instruction);
        public abstract void Restore(Instruction instruction);

        public void TemporaryOffset(Instruction instruction, int offset, Action action)
        {
            void MakeOffset()
            {
                var oldOffset = instruction.GetRegisterOffset(this);
                if (oldOffset == offset) return;
                Add(instruction, offset - oldOffset);
                instruction.RemoveVariableRegister(this);
                instruction.ChangedRegisters.Add(this);
                instruction.SetRegisterOffset(this, offset);
            }

            if (instruction.IsRegisterInUse(this)) {
                MakeOffset();
                action();
            }
            else {
                instruction.BeginRegister(this);
                MakeOffset();
                action();
                instruction.EndRegister(this);
            }
        }
    }
}