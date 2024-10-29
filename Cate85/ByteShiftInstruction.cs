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
        var counterRegister = ByteRegister.FromAddress(0);
        var operandRegisters = ByteRegister.FromAddress(1);
        using (ByteOperation.ReserveRegister(this, counterRegister, RightOperand)) {
            counterRegister.Load(this, RightOperand);
            using (ByteOperation.ReserveRegister(this, operandRegisters)) {
                operandRegisters.Load(this, LeftOperand);
                Compiler.CallExternal(this, functionName);
                operandRegisters.Store(this, DestinationOperand);
            }
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
}