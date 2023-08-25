namespace Inu.Cate.Sc62015
{
    internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            ResultFlags |= Flag.Z;
            if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
                ExchangeOperands();
            }
            if (IncrementOrDecrement())
                return;

            var operation = OperatorId switch
            {
                '+' => "add",
                '-' => "sub",
                _ => throw new NotImplementedException()
            };

            void ViaRegister(Cate.ByteRegister register)
            {
                register.Load(this, LeftOperand);
                if (!Equals(register, ByteRegister.A)) {
                    using (ByteOperation.ReserveRegister(this, ByteRegister.A, RightOperand)) {
                        ByteRegister.A.Load(this, RightOperand);
                        register.Operate(this, operation, true, ByteRegister.A.AsmName);
                        return;
                    }
                }
                register.Operate(this, operation, true, RightOperand);
            }

            var candidates = ByteRegister.AccumulatorAndInternalRam.Where(r => !Equals(r, RightOperand.Register)).ToList();
            if (DestinationOperand.Register is ByteRegister destinationRegister && candidates.Contains(destinationRegister)) {
                ViaRegister(destinationRegister);
                return;
            }
            using var reservation = ByteOperation.ReserveAnyRegister(this, candidates, LeftOperand);
            ViaRegister(reservation.ByteRegister);
            reservation.ByteRegister.Store(this, DestinationOperand);
        }

        protected override int Threshold() => 2;

        protected override void Increment(int count)
        {
            OperateByte("inc\t", count);
            ResultFlags |= Flag.Z;
        }

        protected override void Decrement(int count)
        {
            OperateByte("dec\t", count);
            ResultFlags |= Flag.Z;
        }
    }
}
