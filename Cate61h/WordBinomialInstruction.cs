namespace Inu.Cate.Hd61700
{
    internal class WordBinomialInstruction : Cate.BinomialInstruction
    {
        public WordBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

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
                '+' => "adw",
                '-' => "sbw",
                '|' => "orw",
                '^' => "xrw",
                '&' => "anw",
                _ => throw new NotImplementedException()
            };

            if (DestinationOperand.Register is WordRegister destinationRegister && !Equals(RightOperand.Register, destinationRegister)) {
                ViaRegister(destinationRegister);
                return;
            }

            using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
            {
                var register = reservation.WordRegister;
                ViaRegister(register);
                register.Store(this, DestinationOperand);
            }
            return;

            void ViaRegister(Cate.WordRegister register)
            {
                register.Load(this, LeftOperand);
                register.Operate(this, operation, true, RightOperand);
            }
        }
    }
}
