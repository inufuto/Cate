namespace Inu.Cate.Wdc65816;

internal class ByteAddOrSubtractInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (Equals(RightOperand.Register, ByteRegister.A) && !Equals(LeftOperand.Register, ByteRegister.A) && IsOperatorExchangeable()) {
            ExchangeOperands();
        }
        if (CanIncrementOrDecrement() && IncrementOrDecrement())
            return;

        var operation = OperatorId switch
        {
            '+' => "clc|adc",
            '-' => "sec|sbc",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Flag.Z;

        ByteOperation.OperateByteBinomial(this, operation, true);
    }

    private bool CanIncrementOrDecrement()
    {
        if (LeftOperand is IndirectOperand)
            return false;
        if (DestinationOperand is IndirectOperand)
            return false;
        return true;
    }

    protected override int Threshold() => 3;

    protected override void Increment(int count)
    {
        IncrementOrDecrement("inc", count);
        ResultFlags |= Flag.Z;
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("dec", count);
        ResultFlags |= Flag.Z;
    }

    private void IncrementOrDecrement(string memoryOperation, int count)
    {
        if (DestinationOperand.SameStorage(LeftOperand)) {
            if (!Equals(DestinationOperand.Register, ByteRegister.A)) {
                OperateByte(memoryOperation, count);
                return;
            }
        }
        {
            if (DestinationOperand.Register is ByteRegister byteRegister && !RightOperand.Conflicts(byteRegister)) {
                ViaA();
                return;
            }
        }
        using var reservation = ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand);
        ViaA();
        return;

        void ViaA()
        {
            ByteRegister.A.Load(this, LeftOperand);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + memoryOperation + "\ta");
            }
            ByteRegister.A.Store(this, DestinationOperand);
            RemoveRegisterAssignment(ByteRegister.A);
            AddChanged(ByteRegister.A);
        }
    }
}