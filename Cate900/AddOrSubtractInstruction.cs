namespace Inu.Cate.Tlcs900;

internal class AddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : Cate.AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override int Threshold() => 8;

    public override void BuildAssembly()
    {
        if (IsOperatorExchangeable()) {
            if (LeftOperand is ConstantOperand || DestinationOperand.SameStorage(RightOperand)) {
                ExchangeOperands();
            }
        }
        ResultFlags |= Flag.Z;
        if (IncrementOrDecrement()) return;
        var operation = OperatorId switch
        {
            '+' => "add",
            '-' => "sub",
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
            });
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
        if (DestinationOperand.Register is ByteRegister destinationRegister) {
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
        if (DestinationOperand.Register is WordRegister destinationRegister) {
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



    protected override void Increment(int count)
    {
        IncrementOrDecrement("inc", count);
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("dec", count);
    }

    private void IncrementOrDecrement(string operation, int count)
    {
        if (count == 1 && IsMemoryOperation()) {
            ((Compiler)Compiler).OperateMemory(this, DestinationOperand,
                operand => { WriteLine("\t" + operation + " " + count + "," + operand); });
            return;
        }
        switch (DestinationOperand.Type.ByteCount) {
            case 1:
                IncrementOrDecrementByte(operation, count);
                return;
            case 2:
                IncrementOrDecrementWord(operation, count);
                return;
        }
    }

    private void IncrementOrDecrementByte(string operation, int count)
    {
        if (DestinationOperand.Register is ByteRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
        ViaRegister(reservation.ByteRegister);
        return;

        void ViaRegister(Cate.ByteRegister byteRegister)
        {
            byteRegister.Load(this, LeftOperand);
            WriteLine("\t" + operation + " " + count + "," + byteRegister);
            AddChanged(byteRegister);
            RemoveRegisterAssignment(byteRegister);
            byteRegister.Store(this, DestinationOperand);
        }
    }
    private void IncrementOrDecrementWord(string operation, int count)
    {
        if (DestinationOperand.Register is WordRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, LeftOperand);
            WriteLine("\t" + operation + " " + count + "," + wordRegister);
            AddChanged(wordRegister);
            RemoveRegisterAssignment(wordRegister);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}