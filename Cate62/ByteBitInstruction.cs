namespace Inu.Cate.Sc62015
{
    internal class ByteBitInstruction : BinomialInstruction
    {
        public ByteBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
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
                '|' => "or\t",
                '^' => "xor\t",
                '&' => "and\t",
                _ => throw new NotImplementedException()
            };
            ResultFlags |= Flag.Z;
            {
                if (DestinationOperand is VariableOperand variableOperand &&
                    RightOperand is ConstantOperand constantOperand) {
                    WriteLine("\t" + operation + "[" + variableOperand.Variable.MemoryAddress(variableOperand.Offset) + "]," + constantOperand.MemoryAddress());
                    return;
                }
            }

            void ViaRegister(Cate.ByteRegister register)
            {
                register.Load(this, LeftOperand);
                register.Operate(this, operation, true, RightOperand);
                register.Store(this, DestinationOperand);
            }

            if (DestinationOperand.Register is Cate.ByteRegister byteRegister) {
                ViaRegister(byteRegister);
                return;
            }
            using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
            ViaRegister(reservation.ByteRegister);
        }
    }
}
