using System.Linq;

namespace Inu.Cate;

internal class Binomial(ParameterizableType type, int operatorId, Value leftValue, Value rightValue)
    : Value(type)
{
    private Value leftValue = leftValue;
    private Value rightValue = rightValue;

    public new ParameterizableType Type => (ParameterizableType)base.Type;

    private static readonly int[] ExchangeableOperators = { '|', '^', '&', '+' };

    public override void BuildInstructions(Function function,
        AssignableOperand destinationOperand)
    {
        {
            if (leftValue.IsConstant() && !rightValue.IsConstant() && ExchangeableOperators.Contains(operatorId)) {
                (leftValue, rightValue) = (rightValue, leftValue);
            }
            var leftOperand = leftValue.ToOperand(function);
            if (rightValue is ConstantPointer) {}
            var rightOperand = rightValue.ToOperand(function);
            var instruction = Compiler.Instance.CreateBinomialInstruction(
                function, operatorId,
                destinationOperand,
                leftOperand,
                rightOperand);
            function.Instructions.Add(instruction);
        }
    }

    public override void BuildInstructions(Function function)
    {
        leftValue.BuildInstructions(function);
        rightValue.BuildInstructions(function);
    }
}