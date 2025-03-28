namespace Inu.Cate.Tlcs900;

internal class ShiftInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : Cate.ShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override int Threshold() => 16;
    public override void BuildAssembly()
    {
        if (RightOperand.Register is WordRegister { Low: not null } wordRegister) {
            if (wordRegister.Low.Equals(ByteRegister.A)) {
                ShiftVariableA(Operation());
                return;
            }
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.CopyFrom(this, wordRegister.Low);
                ShiftVariableA(Operation());
                return;
            }
        }
        base.BuildAssembly();
    }

    protected override void ShiftConstant(int count)
    {
        if (count == 0) return;
        var operation = Operation();
        if (count == 1 && IsMemoryOperation()) {
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
                WriteLine("\t" + operation + " " + operand);
            });
            return;
        }
        switch (DestinationOperand.Type.ByteCount) {
            case 1:
                ShiftByteConstant(operation, count);
                return;
            case 2:
                ShiftWordConstant(operation, count);
                return;
        }
        throw new NotImplementedException();
    }

    private string Operation()
    {
        var operation = OperatorId switch
        {
            Keyword.ShiftLeft => ((IntegerType)LeftOperand.Type).Signed ? "sla" : "sll",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed ? "sra" : "srl",
            _ => throw new NotImplementedException()
        };
        return operation;
    }

    private void ShiftByteConstant(string operation, int count)
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
            if (count != 0)
            {
                WriteLine("\t" + operation + " " + count + "," + byteRegister);
            }
            byteRegister.Store(this, DestinationOperand);
        }
    }

    private void ShiftWordConstant(string operation, int count)
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
            if (count != 0)
            {
                WriteLine("\t" + operation + " " + count + "," + wordRegister);
            }
            wordRegister.Store(this, DestinationOperand);
        }
    }

    protected override void ShiftVariable(Operand counterOperand)
    {
        var operation = Operation();
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            ByteRegister.A.Load(this, counterOperand);
            ShiftVariableA(operation);
        }
    }

    private void ShiftVariableA(string operation)
    {
        switch (DestinationOperand.Type.ByteCount) {
            case 1:
                ShiftByteVariable(operation);
                return;
            case 2:
                ShiftWordVariable(operation);
                return;
        }
        throw new NotImplementedException();
    }

    private void ShiftByteVariable(string operation)
    {
        if (DestinationOperand.Register is ByteRegister destinationRegister && !destinationRegister.Equals(ByteRegister.A)) {
            ViaRegister(destinationRegister);
            return;
        }

        var candidates = ByteRegister.All.Where(r => !r.Equals(ByteRegister.A)).Cast<Cate.ByteRegister>().ToList();
        using var reservation = ByteOperation.ReserveAnyRegister(this, candidates, LeftOperand);
        ViaRegister(reservation.ByteRegister);
        return;

        void ViaRegister(Cate.ByteRegister byteRegister)
        {
            byteRegister.Load(this, LeftOperand);
            WriteLine("\t" + operation + " a," + byteRegister);
            byteRegister.Store(this, DestinationOperand);
        }
    }

    private void ShiftWordVariable(string operation)
    {
        if (DestinationOperand.Register is WordRegister destinationRegister && !destinationRegister.Equals(WordRegister.WA)) {
            ViaRegister(destinationRegister);
            return;
        }
        var candidates = WordRegister.All.Where(r => !r.Equals(WordRegister.WA)).Cast<Cate.WordRegister>().ToList();
        using var reservation = WordOperation.ReserveAnyRegister(this, candidates, LeftOperand);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, LeftOperand);
            WriteLine("\t" + operation + " a," + wordRegister);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}