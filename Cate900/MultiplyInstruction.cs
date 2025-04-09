namespace Inu.Cate.Tlcs900;

internal class MultiplyInstruction(
    Function function,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    int rightValue)
    : Cate.MultiplyInstruction(function, destinationOperand, leftOperand, rightValue)
{
    public override void BuildAssembly()
    {
        if (RightValue == 0) {
            if (DestinationOperand.Register is WordRegister wordRegister) {
                wordRegister.LoadConstant(this, 0);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this);
            reservation.WordRegister.LoadConstant(this, 0);
            reservation.WordRegister.Store(this, DestinationOperand);
            return;
        }

        if (LeftOperand.Type.ByteCount != 2)
            throw new NotImplementedException();
        if (DestinationOperand.Register is WordRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        using (var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand)) {
            ViaRegister(reservation.WordRegister);

        }
        return;

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, LeftOperand);
            WriteLine("\tmul " + wordRegister + "," + RightValue);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}