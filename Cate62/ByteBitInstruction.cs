namespace Inu.Cate.Sc62015;

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
            if (LeftOperand.SameStorage(DestinationOperand) && DestinationOperand is VariableOperand variableOperand && RightOperand is ConstantOperand constantOperand) {
                if (Equals(DestinationOperand.Register, ByteRegister.A) || DestinationOperand.Register is ByteInternalRam) {
                    WriteLine("\t" + operation + " " + DestinationOperand.Register.AsmName + "," + constantOperand.MemoryAddress());
                    return;
                }
                if (variableOperand.Register == null)
                {
                    WriteLine("\t" + operation + "[" + variableOperand.Variable.MemoryAddress(variableOperand.Offset) + "]," + constantOperand.MemoryAddress());
                    return;
                }
            }
        }

        var candidates = ByteRegister.AccumulatorAndInternalRam;
        if (DestinationOperand.Register is Cate.ByteRegister byteRegister && candidates.Contains(byteRegister)) {
            ViaRegister(byteRegister);
            return;
        }
        using var reservation = ByteOperation.ReserveAnyRegister(this, candidates, LeftOperand);
        ViaRegister(reservation.ByteRegister);
        return;

        void ViaRegister(Cate.ByteRegister register)
        {
            register.Load(this, LeftOperand);
            register.Operate(this, operation, true, RightOperand);
            register.Store(this, DestinationOperand);
        }
    }
}