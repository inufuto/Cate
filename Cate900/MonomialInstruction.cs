namespace Inu.Cate.Tlcs900;

internal class MonomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand sourceOperand)
    : Cate.MonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        var operation = OperatorId switch
        {
            '-' => "neg ",
            '~' => "cpl ",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Flag.Z;
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
        using var reservation = ByteOperation.ReserveAnyRegister(this, SourceOperand);
        ViaRegister(reservation.ByteRegister);
        return;

        void ViaRegister(Cate.ByteRegister byteRegister)
        {
            byteRegister.Load(this, SourceOperand);
            WriteLine("\t" + operation + " " + byteRegister);
            AddChanged(byteRegister);
            RemoveRegisterAssignment(byteRegister);
            byteRegister.Store(this, DestinationOperand);
        }
    }

    private void OperateWord(string operation)
    {
        if (DestinationOperand.Register is WordRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, SourceOperand);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, SourceOperand);
            WriteLine("\t" + operation + " " + wordRegister);
            AddChanged(wordRegister);
            RemoveRegisterAssignment(wordRegister);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}