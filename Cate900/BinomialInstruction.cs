namespace Inu.Cate.Tlcs900;

internal class BinomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : Cate.BinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (IsOperatorExchangeable()) {
            if (LeftOperand is ConstantOperand || DestinationOperand.SameStorage(RightOperand)) {
                ExchangeOperands();
            }
        }
        var operation = OperatorId switch
        {
            '|' => "or",
            '^' => "xor",
            '&' => "and",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Flag.Z;

        if (IsMemoryOperation() && RightOperand is ConstantOperand constantOperand) {
            switch (DestinationOperand.Type.ByteCount) {
                case 1:
                    break;
                case 2:
                    operation += "w";
                    break;
                default:
                    throw new NotImplementedException();
            }
            ((Compiler)Cate.Compiler.Instance).OperateMemory(this, DestinationOperand, operand =>
            {
                WriteLine("\t" + operation + " " + operand + "," + constantOperand.MemoryAddress());
            }, true);
            return;
        }
        switch (DestinationOperand.Type.ByteCount) {
            case 1:
                OperateByte(operation);
                return;
            case 2:
                OperateWord(operation);
                return;
        }
        throw new NotImplementedException();
    }

    private void OperateByte(string operation)
    {
        if (DestinationOperand.Register is ByteRegister destinationRegister && !RightOperand.Conflicts(destinationRegister)) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
        ViaRegister(reservation.ByteRegister);
        return;

        void ViaRegister(Cate.ByteRegister byteRegister)
        {
            byteRegister.Load(this, LeftOperand);
            byteRegister.Operate(this, operation, true, RightOperand);
            byteRegister.Store(this, DestinationOperand);
        }
    }

    private void OperateWord(string operation)
    {
        if (DestinationOperand.Register is WordRegister destinationRegister && !RightOperand.Conflicts(destinationRegister)) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, LeftOperand);
            wordRegister.Operate(this, operation, true, RightOperand);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}