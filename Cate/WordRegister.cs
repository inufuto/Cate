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
        public virtual void LoadConstant(Instruction instruction, int value)
        {
            if (instruction.IsConstantAssigned(this, value)) {
                instruction.ChangedRegisters.Add(this);
                return;
            }
            LoadConstant(instruction, value.ToString());
            instruction.SetRegisterConstant(this, value);
        }
        public abstract void LoadFromMemory(Instruction instruction, string label);
        public abstract void StoreToMemory(Instruction instruction, string label);
        public abstract void Load(Instruction instruction, Operand sourceOperand);
        public abstract void Store(Instruction instruction, AssignableOperand destinationOperand);
        public abstract void LoadFromMemory(Instruction instruction, Variable variable, int offset);


        public abstract void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset);
        public abstract void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset);

        public abstract void CopyFrom(Instruction instruction, WordRegister sourceRegister);


        public abstract void Operate(Instruction instruction, string operation, bool change, Operand operand);

        public virtual void TemporaryOffset(Instruction instruction, int offset, Action action)
        {
            if (instruction.IsRegisterInUse(this)) {
                var changed = instruction.ChangedRegisters.Contains(this);
                Add(instruction, offset);
                action();
                if (!changed) {
                    Add(instruction, -offset);
                    instruction.ChangedRegisters.Remove(this);
                }
            }
            else {
                var changed = instruction.ChangedRegisters.Contains(this);
                instruction.BeginRegister(this);
                Add(instruction, offset);
                instruction.RemoveRegisterAssignment(this);
                instruction.ChangedRegisters.Add(this);
                action();
                Add(instruction, -offset);
                instruction.EndRegister(this);
                if (!changed) {
                    instruction.ChangedRegisters.Remove(this);
                }
            }
        }
    }
}