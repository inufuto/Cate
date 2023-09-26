namespace Inu.Cate.Hd61700
{
    internal class ByteBinomialInstruction : Cate.BinomialInstruction
    {
        public ByteBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            if (
                !Equals(RightOperand.Register, null) &&
                Equals(LeftOperand.Register, null) &&
                IsOperatorExchangeable()
            ) {
                ExchangeOperands();
            }

            var operation = OperatorId switch
            {
                '+' => "ad",
                '-' => "sb",
                '|' => "or",
                '^' => "xr",
                '&' => "an",
                _ => throw new NotImplementedException()
            };

            if (DestinationOperand.Register is ByteRegister destinationRegister) {
                ViaRegister(destinationRegister);
                return;
            }

            using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
            {
                var register = reservation.ByteRegister;
                ViaRegister(register);
                register.Store(this, DestinationOperand);
            }
            return;

            void ViaRegister(Cate.ByteRegister register)
            {
                register.Load(this, LeftOperand);
                register.Operate(this, operation, true, RightOperand);
            }
        }
    }
}
