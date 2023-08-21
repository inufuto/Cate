using System;
using System.IO;
using System.Linq;

namespace Inu.Cate
{
    public abstract class Register : IComparable<Register>
    {
        public readonly int Id;
        public readonly int ByteCount;
        public readonly string Name;

        protected Register(int id, int byteCount, string name)
        {
            Id = id;
            ByteCount = byteCount;
            Name = name;
        }

        public override string ToString() => Name;
        public virtual string AsmName => Name;

        protected static ByteOperation ByteOperation => Compiler.Instance.ByteOperation;
        protected static WordOperation WordOperation => Compiler.Instance.WordOperation;
        protected static PointerOperation PointerOperation => Compiler.Instance.PointerOperation;

        public int CompareTo(Register? other)
        {
            return other != null ? Id.CompareTo(other.Id) : int.MaxValue;
        }

        public override bool Equals(object? obj)
        {
            return obj is Register register && register.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public virtual bool Conflicts(Register? register)
        {
            return Equals(register, this);
            //return Equals(register, this) || register is ByteRegister byteRegister && Contains(byteRegister);
        }

        public virtual bool Matches(Register register)
        {
            return Equals(register, this);
        }

        public abstract void Save(StreamWriter writer, string? comment, bool jump, int tabCount);

        public abstract void Restore(StreamWriter writer, string? comment, bool jump, int tabCount);
        public abstract void Save(Instruction instruction);
        public abstract void Restore(Instruction instruction);

        public abstract void LoadConstant(Instruction instruction, string value);

        public abstract void LoadFromMemory(Instruction instruction, string label);
        public abstract void StoreToMemory(Instruction instruction, string label);

        public virtual void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            LoadFromMemory(instruction, variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
            instruction.AddChanged(this);
        }

        public virtual void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            StoreToMemory(instruction, variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
        }

        public abstract void LoadIndirect(Instruction instruction, PointerRegister pointerRegister, int offset);

        public abstract void StoreIndirect(Instruction instruction, PointerRegister pointerRegister, int offset);

        public virtual void LoadIndirect(Instruction instruction, Variable pointer, int offset)
        {
            var allCandidates = PointerOperation.Registers.Where(r => !r.Conflicts(this)).ToList();
            var unReserved = allCandidates.Where(r => !instruction.IsRegisterReserved(r)).ToList();
            var candidates = unReserved.Where(r => r.IsOffsetInRange(offset)).ToList();
            if (candidates.Count == 0) {
                candidates = unReserved;
            }
            if (candidates.Count == 0) {
                candidates = allCandidates;
            }
            if (candidates.Count == 0) {
                candidates = PointerOperation.Registers;
            }
            using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
            reservation.PointerRegister.LoadFromMemory(instruction, pointer, 0);
            LoadIndirect(instruction, reservation.PointerRegister, offset);
        }
        public virtual void StoreIndirect(Instruction instruction, Variable pointer, int offset)
        {
            var register = instruction.GetVariableRegister(pointer, 0, r => r is PointerRegister p && p.IsOffsetInRange(offset)) ??
                           instruction.GetVariableRegister(pointer, 0, r => r is PointerRegister p && p.IsOffsetInRange(0));
            if (register is PointerRegister pointerRegister && (Equals(pointerRegister, pointer.Register) || pointerRegister.IsOffsetInRange(offset))) {
                StoreIndirect(instruction, pointerRegister, offset);
                return;
            }

            var pointerRegisters = PointerOperation.RegistersToOffset(offset);
            if (pointerRegisters.Count == 0) {
                pointerRegisters = PointerOperation.Registers;
            }
            var reservation = PointerOperation.ReserveAnyRegister(instruction, pointerRegisters);
            reservation.PointerRegister.LoadFromMemory(instruction, pointer, 0);
            StoreIndirect(instruction, reservation.PointerRegister, offset);
        }
    }
}
