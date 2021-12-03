using System;

namespace Inu.Cate.Mc6809
{
    internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override void BuildAssembly()
        {
            if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
                ExchangeOperands();
            }
            if (IncrementOrDecrement())
                return;

            string operation = OperatorId switch
            {
                '+' => "add",
                '-' => "sub",
                _ => throw new NotImplementedException()
            };
            ResultFlags |= Flag.Z;

            ByteOperation.OperateByteBinomial(this, operation, true);
        }

        protected override int Threshold() => 1;
        protected override void Increment(int count)
        {
            OperateByte("inc", count);
            ResultFlags |= Flag.Z;
        }

        protected override void Decrement(int count)
        {
            OperateByte("dec", count);
            ResultFlags |= Flag.Z;
        }
    }
}