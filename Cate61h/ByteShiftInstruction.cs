namespace Inu.Cate.Hd61700;

internal class ByteShiftInstruction : Cate.ByteShiftInstruction
{
    public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

    protected override int Threshold() => 4;

    protected override void ShiftConstant(int count)
    {
        if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
            var counterRegister = ByteRegister.Registers[1];
            using (ByteOperation.ReserveRegister(this, counterRegister)) {
                counterRegister.LoadConstant(this, count);
                CallExternal("cate.ShiftSignedByteRight");
            }
            return;
        }
        base.ShiftConstant(count);
    }

    protected override void ShiftVariable(Operand counterOperand)
    {
        var functionName = OperatorId switch
        {
            Keyword.ShiftLeft => "cate.ShiftByteLeft",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                ? "cate.ShiftSignedByteRight"
                : "cate.ShiftUnsignedByteRight",
            _ => throw new NotImplementedException()
        };
        var counterRegister = ByteRegister.Registers[1];
        using (ByteOperation.ReserveRegister(this, counterRegister)) {
            if (RightOperand.Register is WordRegister wordRegister) {
                WriteLine("\tld " + counterRegister.AsmName + "," + wordRegister.AsmName);
            }
            else {
                counterRegister.Load(this, RightOperand);
            }
            CallExternal(functionName);
        }
    }

    protected override string Operation()
    {
        return OperatorId switch
        {
            Keyword.ShiftLeft => "biu",
            Keyword.ShiftRight when !((IntegerType)LeftOperand.Type).Signed => "bid",
            _ => throw new NotImplementedException()
        };
    }
    private void CallExternal(string functionName)
    {
        var operandRegister = ByteRegister.Registers[0];

        if (Equals(DestinationOperand.Register, operandRegister)) {
            Call();
            return;
        }
        using (ByteOperation.ReserveRegister(this, operandRegister)) {
            Call();
        }
        return;

        void Call()
        {
            operandRegister.Load(this, LeftOperand);
            Compiler.CallExternal(this, functionName);
            RemoveRegisterAssignment(operandRegister);
            AddChanged(operandRegister);
            var counterRegister = ByteRegister.Registers[1];
            RemoveRegisterAssignment(counterRegister);
            AddChanged(counterRegister);
            operandRegister.Store(this, DestinationOperand);
        }
    }
}