using System;
using System.Diagnostics;
using Inu.Language;

namespace Inu.Cate
{
    public class IntegerType : ParameterizableType
    {
        public static readonly IntegerType ByteType = new(1, false);
        public static readonly IntegerType SignedByteType = new(1, true);
        public static readonly IntegerType WordType = new(2, false);
        public static readonly IntegerType SignedWordType = new(2, true);

        public static IntegerType IntegerTypeOf(int value)
        {
            if (value >= 0x8000)
                return WordType;
            if (value >= 0x100)
                return SignedWordType;
            if (value >= 0x80)
                return ByteType;
            if (value >= -128)
                return SignedByteType;
            return SignedWordType;
        }

        public override int ByteCount { get; }
        public override int Incremental => 1;
        public readonly bool Signed;

        public IntegerType(int byteCount, bool signed)
        {
            ByteCount = byteCount;
            Signed = signed;
        }

        public override bool Equals(object? obj)
        {
            return obj is IntegerType integerType &&
                   (ByteCount == integerType.ByteCount && Signed == integerType.Signed);
        }

        public override int GetHashCode()
        {
            return ByteCount.GetHashCode() + Signed.GetHashCode();
        }

        public override string ToString()
        {
            return (Signed ? "s" : "") + (ByteCount == 1 ? "byte" : "word");
        }

        public override Constant DefaultValue()
        {
            return new ConstantInteger(this, 0);
        }

        public override Constant? ParseConstant(Compiler compiler)
        {
            var constantInteger = compiler.ParseConstantInteger();
            return constantInteger.Type.Equals(this) ? constantInteger : new ConstantInteger(this, constantInteger.IntegerValue);
        }

        public override Value? BinomialResult(SourcePosition position, int operatorId, Value leftValue,
            Value rightValue)
        {
            switch (operatorId) {
                case '|':
                case '^':
                case '&':
                case '+':
                case '-': {
                        if (leftValue.Type is IntegerType leftValueType && rightValue.Type is IntegerType rightValueType) {
                            ParameterizableType? resultType;
                            if (leftValue.IsConstant() && !rightValue.IsConstant()) {
                                resultType = rightValueType;
                            }
                            else if (!leftValue.IsConstant() && rightValue.IsConstant()) {
                                resultType = leftValueType;
                            }
                            else {
                                resultType = leftValueType.CombineType(rightValueType) as ParameterizableType;
                            }
                            Debug.Assert(resultType != null);
                            var leftConvertedValue = leftValue.ConvertTypeTo(resultType);
                            var rightConvertedValue = rightValue.ConvertTypeTo(resultType);
                            Debug.Assert(leftConvertedValue != null && rightConvertedValue != null);
                            return new Binomial(resultType, operatorId, leftConvertedValue, rightConvertedValue);
                        }
                        break;
                    }
                case Keyword.ShiftLeft:
                case Keyword.ShiftRight: {
                        if (leftValue.Type is ParameterizableType resultType) {
                            return new Binomial(resultType, operatorId, leftValue, rightValue);
                        }
                        break;
                    }
                case Keyword.Equal:
                case Keyword.NotEqual:
                case '<':
                case '>':
                case Keyword.LessEqual:
                case Keyword.GreaterEqual: {
                        if (leftValue.Type.CombineType(rightValue.Type) is not ParameterizableType commonType)
                            break;
                        var leftConvertedValue = leftValue.ConvertTypeTo(commonType);
                        var rightConvertedValue = rightValue.ConvertTypeTo(commonType);
                        Debug.Assert(leftConvertedValue != null && rightConvertedValue != null);
                        return new Comparison(operatorId, leftConvertedValue, rightConvertedValue);
                    }
            }
            return base.BinomialResult(position, operatorId, leftValue, rightValue);
        }

        public override Value? MonomialResult(SourcePosition position, int operatorId, Value value)
        {
            return operatorId switch
            {
                '+' => value,
                '-' or '~' => new Monomial(this, operatorId, value),
                _ => base.MonomialResult(position, operatorId, value)
            };
        }

        public override Value? ConvertType(Value value, Type type)
        {
            return type switch
            {
                IntegerType integerType when ByteCount == integerType.ByteCount => new TypeChange(integerType, value),
                IntegerType integerType => new Resize(value, this, integerType),
                _ => base.ConvertType(value, type)
            };
        }

        public override Type? CombineType(Type type)
        {
            if (type is not IntegerType integerType)
                return base.CombineType(type);
            var signed = Signed && integerType.Signed;
            var byteCount = Math.Max(ByteCount, integerType.ByteCount);
            if (signed) {
                return byteCount == 1 ? SignedByteType : SignedWordType;
            }
            return byteCount == 1 ? ByteType : WordType;
        }
    }
}