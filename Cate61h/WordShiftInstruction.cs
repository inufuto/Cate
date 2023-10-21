namespace Inu.Cate.Hd61700;

internal class WordShiftInstruction : Cate.WordShiftInstruction
{
    public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

    protected override void ShiftConstant(int count)
    {
        if (OperatorId == Keyword.ShiftRight) {
            if (((IntegerType)LeftOperand.Type).Signed) {
                var counterRegister = ByteRegister.Registers[1];
                using (ByteOperation.ReserveRegister(this, counterRegister)) {
                    counterRegister.LoadConstant(this, count);
                    CallExternal("cate.ShiftSignedWordRight");
                }
            }
            else {

                Operate(count, operandRegister => "bidw " + operandRegister.HighByteName);
            }
        }
        else if (OperatorId == Keyword.ShiftLeft) {
            Operate(count, operandRegister => "biuw " + operandRegister.AsmName);
        }
        throw new NotImplementedException();
    }

    private void Operate(int count, Func<WordRegister, string> toName)
    {
        if (DestinationOperand.Register is WordRegister destinationOperandRegister) {
            ViaRegister(destinationOperandRegister);
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(this);
            var wordRegister = reservation.WordRegister;
            ViaRegister(wordRegister);
        }

        return;

        void ViaRegister(Cate.WordRegister operandRegister)
        {
            operandRegister.Load(this, LeftOperand);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + toName);
            }
            operandRegister.Store(this, DestinationOperand);
        }
    }


    protected override void ShiftVariable(Operand counterOperand)
    {
        var functionName = OperatorId switch
        {
            Keyword.ShiftLeft => "cate.ShiftWordLeft",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                ? "cate.ShiftSignedWordRight"
                : "cate.ShiftUnsignedWordRight",
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
    private void CallExternal(string functionName)
    {
        var operandRegister = WordRegister.Registers[0];

        if (Equals(DestinationOperand.Register, operandRegister)) {
            Call();
            return;
        }
        using (WordOperation.ReserveRegister(this, operandRegister)) {
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