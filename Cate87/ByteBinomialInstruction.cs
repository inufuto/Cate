using System;

namespace Inu.Cate.MuCom87;

internal class ByteBinomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : BinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (RightOperand is IntegerOperand integerOperand && integerOperand.IntegerValue == 0) {
            switch (OperatorId) {
                case '|':
                case '^':
                case '+':
                case '-':
                    if (LeftOperand.SameStorage(DestinationOperand)) return;
                    using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                        ByteRegister.A.Load(this, LeftOperand);
                        ByteRegister.A.Store(this, DestinationOperand);
                    }
                    return;
            }
        }

        if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
            ExchangeOperands();
        }

        var operation = OperatorId switch
        {
            '|' => "ora|ori",
            '^' => "xra|xri",
            '&' => "ana|ani",
            '+' => "add|adi",
            '-' => "sub|sui",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Flag.Z;

        ByteOperation.OperateByteBinomial(this, operation, true);
    }
}