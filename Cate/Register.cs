using System;
using System.IO;

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

        protected readonly WordOperation WordOperation = Compiler.Instance.WordOperation;
        protected readonly ByteOperation ByteOperation = Compiler.Instance.ByteOperation;

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

        //public virtual bool Contains(Register register)
        //{
        //    return Equals(register);
        //}

        public abstract void Save(StreamWriter writer, string? comment, bool jump, int tabCount);

        public abstract void Restore(StreamWriter writer, string? comment, bool jump, int tabCount);
        public abstract void Save(Instruction instruction);
        public abstract void Restore(Instruction instruction);
    }
}
