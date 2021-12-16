using System;

namespace Inu.Cate.MuCom87
{
    internal class ByteBinomialInstruction : BinomialInstruction
    {
        public ByteBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
                ExchangeOperands();
            }

            string operation = OperatorId switch
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
}
