namespace Inu.Cate.Sm83;

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
            Keyword.ShiftLeft => "cate.ShiftLeftA",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                ? "cate.ShiftRightSignedA"
                : "cate.ShiftRightA",
            _ => throw new NotImplementedException()
        };
        using (ByteOperation.ReserveRegister(this, ByteRegister.B, RightOperand)) {
            ByteRegister.B.Load(this, RightOperand);
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, LeftOperand);
                Compiler.CallExternal(this, functionName);
                ByteRegister.A.Store(this, DestinationOperand);
            }
        }
    }

    protected override string Operation()
    {
        return OperatorId switch
        {
            Keyword.ShiftLeft => "sla\t",
            Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => "sra\t",
            Keyword.ShiftRight => "srl\t",
            _ => throw new NotImplementedException()
        };
    }
}