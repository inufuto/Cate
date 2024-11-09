namespace Inu.Cate.Sm85;

internal class ByteMonomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand sourceOperand)
    : MonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        var operation = OperatorId switch
        {
            '-' => "neg",
            '~' => "com",
            _ => throw new NotImplementedException()
        };
        if (DestinationOperand.Register is ByteRegister byteRegister) {
            ViaRegister(byteRegister);
            return;
        }
        using var reservation = ByteOperation.ReserveAnyRegister(this, SourceOperand);
        ViaRegister(reservation.ByteRegister);
        return;

        void ViaRegister(Cate.ByteRegister register)
        {
            register.Load(this, SourceOperand);
            WriteLine("\t" + operation + "\t" + register);
            register.Store(this, DestinationOperand);
        }
    }
}