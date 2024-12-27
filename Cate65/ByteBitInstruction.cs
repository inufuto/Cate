using System;

namespace Inu.Cate.Mos6502;

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
            '|' => "ora",
            '^' => "eor",
            '&' => "and",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Flag.Z;

        ByteOperation.OperateByteBinomial(this, operation, true);
    }
}