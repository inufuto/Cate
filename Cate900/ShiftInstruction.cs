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
    //public override void BuildAssembly()
    //{
    //    if (RightOperand.Register is WordRegister { Low: not null } wordRegister) {
    //        if (wordRegister.Low.Equals(ByteRegister.A)) {
    //            ShiftVariableA(Operation());
    //            return;
    //        }
    //        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
    //            ByteRegister.A.CopyFrom(this, wordRegister.Low);
    //            ShiftVariableA(Operation());
    //            return;
    //        }
    //    }
    //    base.BuildAssembly();
    //}

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
            if (count != 0) {
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
            if (count != 0) {
                WriteLine("\t" + operation + " " + count + "," + wordRegister);
            }
            wordRegister.Store(this, DestinationOperand);
        }
    }

    protected override void ShiftVariable(Operand counterOperand)
    {
        switch (DestinationOperand.Type.ByteCount) {
            case 1:
                ShiftByteVariable(counterOperand);
                break;
            case 2:
                ShiftWordVariable(counterOperand);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void ShiftByteVariable(Operand counterOperand)
    {
        if (counterOperand.Register is ByteRegister counterRegister && counterRegister.Equals(ByteRegister.A)) {
            ViaAnyRegister();
            return;
        }
        if (DestinationOperand.Register is ByteRegister destinationRegister) {
            if (destinationRegister.Equals(ByteRegister.A)) {
                ViaAnyRegister();
                return;
            }
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ViaRegister(destinationRegister);
            }
            return;
        }

        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            ViaAnyRegister();
        }
        return;

        void ViaAnyRegister()
        {
            var candidates = ByteRegister.All.Where(r => !r.Equals(ByteRegister.A)).Cast<Cate.ByteRegister>().ToList();
            using var reservation = ByteOperation.ReserveAnyRegister(this, candidates, LeftOperand);
            ViaRegister(reservation.ByteRegister);
        }

        void ViaRegister(Cate.ByteRegister byteRegister)
        {
            ByteRegister.A.Load(this, counterOperand);
            byteRegister.Load(this, LeftOperand);
            WriteLine("\tor a,a");
            WriteLine("\tif nz");
            WriteLine("\t" + Operation() + " a," + byteRegister);
            WriteLine("\tendif");
            byteRegister.Store(this, DestinationOperand);
        }
    }

    private void ShiftWordVariable(Operand counterOperand)
    {
        if (counterOperand.Register is ByteRegister counterRegister && counterRegister.Equals(ByteRegister.A)) {
            ViaAnyRegister();
            return;
        }
        if (DestinationOperand.Register is WordRegister destinationRegister) {
            if (destinationRegister.Equals(WordRegister.WA)) {
                ViaAnyRegister();
                return;
            }
            using (WordOperation.ReserveRegister(this, WordRegister.WA)) {
                ViaRegister(destinationRegister);
            }
            return;
        }

        using (WordOperation.ReserveRegister(this, WordRegister.WA)) {
            ViaAnyRegister();
        }
        return;

        void ViaAnyRegister()
        {
            var candidates = WordRegister.All.Where(r => !r.Equals(WordRegister.WA)).Cast<Cate.WordRegister>().ToList();
            using var reservation = WordOperation.ReserveAnyRegister(this, candidates, LeftOperand);
            ViaRegister(reservation.WordRegister);
        }

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            ByteRegister.A.Load(this, counterOperand);
            wordRegister.Load(this, LeftOperand);
            WriteLine("\tor a,a");
            WriteLine("\tif nz");
            WriteLine("\t" + Operation() + " a," + wordRegister);
            WriteLine("\tendif");
            wordRegister.Store(this, DestinationOperand);
        }
    }
}