namespace Inu.Cate.Sm85;

internal class WordMonomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand sourceOperand)
    : MonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        if (DestinationOperand.Register is WordRegister wordRegister) {
            ViaRegister(wordRegister);
            return;
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, SourceOperand);
        ViaRegister(reservation.WordRegister);

        void ViaRegister(Cate.WordRegister register)
        {
            register.Load(this, SourceOperand);
            WriteLine("\tcom\t" + register.Low);
            WriteLine("\tcom\t" + register.High);
            if (OperatorId == '-') {
                WriteLine("\tincw\t" + register);
            }
            register.Store(this, DestinationOperand);
        }
    }
}