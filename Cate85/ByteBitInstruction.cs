namespace Inu.Cate.Sm85;

internal class ByteBitInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : BinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
            ExchangeOperands();
        }
        var operation = OperatorId switch
        {
            '|' => "or",
            '^' => "xor",
            '&' => "and",
            _ => throw new NotImplementedException()
        };
        ByteOperation.OperateByteBinomial(this, operation, true);
        ResultFlags |= Flag.Z;
    }
}