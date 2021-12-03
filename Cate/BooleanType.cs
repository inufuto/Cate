using System;
using Inu.Language;

namespace Inu.Cate
{
    class BooleanType : ParameterizableType
    {
        public static readonly BooleanType Type = new BooleanType();

        public override int ByteCount => 1;
        public override int Incremental => throw new NotImplementedException();

        public override bool Equals(object? obj)
        {
            return obj is BooleanType;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override Constant DefaultValue()
        {
            return new ConstantBoolean(false);
        }

        public override Constant? ParseConstant(Compiler compiler)
        {
            return compiler.ParseConstantBoolean();
        }

        public override Value? BinomialResult(SourcePosition position, int operatorId, Value leftValue,
            Value rightValue)
        {
            var leftBooleanValue = leftValue.ToBooleanValue();
            var rightBooleanValue = rightValue.ToBooleanValue();

            if (leftBooleanValue == null || rightBooleanValue == null) {
                return base.BinomialResult(position, operatorId, leftValue, rightValue);
            }
            switch (operatorId) {
                case Keyword.LogicalOr: {
                        if (leftValue is ConstantBoolean leftConstant) {
                            return leftConstant.BooleanValue.Value ? leftValue : rightValue;
                        }
                        if (rightValue is ConstantBoolean rightConstant) {
                            return rightConstant.BooleanValue.Value ? rightValue : leftValue;
                        }
                        return new LogicalBinomial(operatorId, leftBooleanValue, rightBooleanValue);
                    }
                case Keyword.LogicalAnd: {
                        if (leftValue is ConstantBoolean leftConstant) {
                            return leftConstant.BooleanValue.Value ? rightValue : leftValue;
                        }
                        if (rightValue is ConstantBoolean rightConstant) {
                            return rightConstant.BooleanValue.Value ? leftValue : rightValue;
                        }
                        return new LogicalBinomial(operatorId, leftBooleanValue, rightBooleanValue);
                    }
            }
            return base.BinomialResult(position, operatorId, leftValue, rightValue);
        }

        public override Value? MonomialResult(SourcePosition position, int operatorId, Value value)
        {
            var booleanValue = value.ToBooleanValue();
            if (operatorId == '!' && booleanValue != null) {
                return new LogicalNot(booleanValue);
            }
            return base.MonomialResult(position, operatorId, value);
        }
    }
}