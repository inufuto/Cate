using System;

namespace Inu.Cate.Mc6809
{
    internal class ByteBitInstruction : BinomialInstruction
    {
        public ByteBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override void BuildAssembly()
        {
            if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
                ExchangeOperands();
            }

            string operation = OperatorId switch
            {
                '|' => "or",
                '^' => "eor",
                '&' => "and",
                _ => throw new NotImplementedException()
            };
            ByteOperation.OperateByteBinomial(this, operation, true);
            ResultFlags |= Flag.Z;
        }
    }
}