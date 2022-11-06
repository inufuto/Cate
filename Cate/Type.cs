using System.Diagnostics;
using Inu.Language;

namespace Inu.Cate
{
    public abstract class Type
    {
        public abstract int ByteCount { get; }

        public virtual bool IsVoid() => false;

        public abstract int Incremental { get; }
        public abstract int MaxElementSize { get; }

        public abstract Constant DefaultValue();
        public abstract Constant? ParseConstant(Compiler compiler);

        public virtual Value? BinomialResult(SourcePosition position, int operatorId, Value leftValue, Value rightValue)
        {
            return null;
        }

        public virtual Value? MonomialResult(SourcePosition position, int operatorId, Value value)
        {
            return null;
        }

        public virtual Value? ConvertType(Value value, Type type)
        {
            return null;
        }

        public virtual Value? Cast(Value value, Type type)
        {
            return null;
        }

        public virtual Type? CombineType(Type type)
        {
            return Equals(type) ? this : null;
        }
    }
}
