using System.Diagnostics;
using System.Runtime.InteropServices;
using Inu.Language;

namespace Inu.Cate
{
    public class PointerType : ParameterizableType
    {
        public readonly Type ElementType;

        public PointerType(Type elementType)
        {
            ElementType = elementType;
        }

        public override bool Equals(object? obj)
        {
            return obj is PointerType pointerType && (ElementType.Equals(pointerType.ElementType) || pointerType.ElementType is VoidType);
        }

        public override int GetHashCode()
        {
            return ElementType.GetHashCode();
        }

        public override int ByteCount => 2;
        public override int Incremental => ElementType.ByteCount;

        public override Constant DefaultValue()
        {
            return new ConstantPointer(this, null, 0);
        }

        public override Constant? ParseConstant(Compiler compiler)
        {
            return compiler.ParseConstantPointer();
        }

        public override Value? BinomialResult(SourcePosition position, int operatorId, Value leftValue,
            Value rightValue)
        {
            switch (operatorId) {
                case '+':
                case '-': {
                        Value offset;
                        if (rightValue is ConstantInteger constantInteger) {
                            if (constantInteger.IntegerValue == 0) {
                                return leftValue;
                            }
                            offset = new ConstantInteger(IntegerType.SignedWordType, constantInteger.IntegerValue * ElementType.ByteCount);
                        }
                        else {
                            var convertedValue = rightValue.ConvertTypeTo(IntegerType.SignedWordType);
                            if (convertedValue == null) return null;
                            offset = ElementType.ByteCount == 1 ? convertedValue : new Multiplication(convertedValue, ElementType.ByteCount);
                        }
                        return new Binomial(this, operatorId, leftValue, offset);
                    }
                case Keyword.Equal:
                case Keyword.NotEqual:
                case '<':
                case '>':
                case Keyword.LessEqual:
                case Keyword.GreaterEqual: {
                        return new Comparison(operatorId, leftValue, rightValue);
                    }
            }
            return base.BinomialResult(position, operatorId, leftValue, rightValue);
        }
    }
}