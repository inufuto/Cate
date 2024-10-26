namespace Inu.Cate.Sm83;

internal class ByteBitInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : BinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (
            Equals(register, ByteRegister.A) &&
            !IsOperatorExchangeable() &&
            RightOperand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)
        )
            return null;
        return base.RegisterAdaptability(variable, register);
    }

    public override void BuildAssembly()
    {
        if (
            Equals(RightOperand.Register, ByteRegister.A) &&
            !Equals(LeftOperand.Register, ByteRegister.A) &&
            IsOperatorExchangeable()
        ) {
            ExchangeOperands();
        }

        var operation = OperatorId switch
        {
            '|' => "or",
            '^' => "xor",
            '&' => "and",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Instruction.Flag.Z;

        using (ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
            ByteRegister.A.Load(this, LeftOperand);
            ByteRegister.A.Operate(this, operation, true, RightOperand);
            ByteRegister.A.Store(this, DestinationOperand);
            AddChanged(ByteRegister.A);
        }
    }
}