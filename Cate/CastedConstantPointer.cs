using System.IO;
using Inu.Language;

namespace Inu.Cate
{
    class CastedConstantPointer : Constant
    {
        public readonly int IntegerValue;

        public CastedConstantPointer(PointerType type, int integerValue) : base(type)
        {
            IntegerValue = integerValue;
        }

        private new PointerType Type => (PointerType)base.Type;

        public override bool Equals(object? obj)
        {
            return obj is CastedConstantPointer literalPointer && IntegerValue == literalPointer.IntegerValue;
        }

        public override int GetHashCode()
        {
            return IntegerValue.GetHashCode();
        }

        public override void WriteAssembly(StreamWriter writer)
        {
            writer.WriteLine("\tdefw " + IntegerValue);
        }

        public override Operand ToOperand()
        {
            return new IntegerOperand(Type, IntegerValue);
        }

        public override Value? BinomialResult(SourcePosition position, int operatorId, Value rightValue)
        {
            if (rightValue is ConstantInteger rightConstant) {
                switch (operatorId) {
                    case '+':
                        return new CastedConstantPointer(Type, IntegerValue + rightConstant.IntegerValue * Type.ElementType.ByteCount);
                    case '-':
                        return new CastedConstantPointer(Type, IntegerValue - rightConstant.IntegerValue * Type.ElementType.ByteCount);
                }
            }
            return base.BinomialResult(position, operatorId, rightValue);
        }

        public override Value? CastTo(Type type)
        {
            if (type is PointerType pointerType) {
                return new CastedConstantPointer(pointerType, IntegerValue);
            }
            return base.CastTo(type);
        }
    }
}