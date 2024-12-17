namespace Inu.Cate.Wdc65816;

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
        if (Equals(RightOperand.Register, ByteRegister.A)) {
            StoreCount();
        }
        else {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                StoreCount();
            }
        }
        CallExternal(functionName);
        Compiler.AddExternalName(Wdc65816.Compiler.TemporaryCountLabel);
        return;

        void StoreCount()
        {
            ByteRegister.A.Load(this, RightOperand);
            ByteRegister.A.MakeSize(this);
            WriteLine("\tsta\t<" + Wdc65816.Compiler.TemporaryCountLabel);
        }
    }

    private void CallExternal(string functionName)
    {
        if (Equals(DestinationOperand.Register, ByteRegister.A)) {
            Call();
            return;
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            Call();
        }
        return;

        void Call()
        {
            ByteRegister.A.Load(this, LeftOperand);
            Compiler.CallExternal(this, functionName);
            RemoveRegisterAssignment(ByteRegister.A);
            AddChanged(ByteRegister.A);
            ByteRegister.A.Store(this, DestinationOperand);
        }
    }

    protected override string Operation()
    {
        return OperatorId switch
        {
            Keyword.ShiftLeft => "asl",
            Keyword.ShiftRight when !((IntegerType)LeftOperand.Type).Signed => "lsr",
            _ => throw new NotImplementedException()
        };
    }
}