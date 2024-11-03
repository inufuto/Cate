namespace Inu.Cate.Sm85;

internal class ByteShiftInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : Cate.ByteShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override void ShiftVariable(Operand counterOperand)
    {
        var functionName = OperatorId switch
        {
            Keyword.ShiftLeft => "cate.ShiftLeftByte",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                ? "cate.ShiftRightSignedByte"
                : "cate.ShiftRightByte",
            _ => throw new NotImplementedException()
        };
        var counterRegister = ByteRegister.FromAddress(3);
        var valueRegister = ByteRegister.FromAddress(1);
        using (ByteOperation.ReserveRegister(this, counterRegister, RightOperand)) {
            counterRegister.Load(this, RightOperand);
            if (DestinationOperand.Register != null && DestinationOperand.Register.Equals(valueRegister)) {
                Call();
            }
            else {
                using (ByteOperation.ReserveRegister(this, valueRegister, LeftOperand)) {
                    Call();
                }
            }
        }
        return;

        void Call()
        {
            valueRegister.Load(this, LeftOperand);
            Compiler.CallExternal(this, functionName);
            valueRegister.Store(this, DestinationOperand);
        }
    }

    protected override string Operation()
    {
        return OperatorId switch
        {
            Keyword.ShiftLeft => "sll",
            Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => "sra",
            Keyword.ShiftRight => "srl",
            _ => throw new NotImplementedException()
        };
    }

    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        {
            if (RightOperand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)) {
                if (register.Conflicts(ByteRegister.FromAddress(3))) {
                    return 1;
                }
            }
        }
        return base.RegisterAdaptability(variable, register);
    }
}