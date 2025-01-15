using System.IO;
using Inu.Language;

namespace Inu.Cate;

public class ConstantBoolean(bool booleanValue) : Constant(BooleanType.Type)
{
    public readonly ConstantBooleanValue BooleanValue = new(booleanValue);

    public override void WriteAssembly(StreamWriter writer)
    {
        writer.WriteLine("\tdefb " + BooleanValue.ToInteger());
    }

    public override Operand ToOperand()
    {
        return BooleanValue.ToOperand();
    }

    public override Value? BinomialResult(SourcePosition position, int operatorId, Value rightValue)
    {
        switch (operatorId) {
            case Keyword.LogicalOr: {
                if (BooleanValue.Value) {
                    return this;
                }
                if (rightValue is ConstantBoolean rightConstant) {
                    if (rightConstant.BooleanValue.Value) {
                        return rightConstant;
                    }
                }
                break;
            }
            case Keyword.LogicalAnd: {
                if (!BooleanValue.Value) {
                    return this;
                }

                if (rightValue is ConstantBoolean rightConstant) {
                    if (!rightConstant.BooleanValue.Value) {
                        return rightConstant;
                    }
                }
                break;
            }
        }
        return base.BinomialResult(position, operatorId, rightValue);
    }

    public override Value? MonomialResult(SourcePosition position, int operatorId)
    {
        return operatorId == '!' ? new ConstantBoolean(!BooleanValue.Value) : base.MonomialResult(position, operatorId);
    }

    public override BooleanValue? ToBooleanValue() => BooleanValue;
}