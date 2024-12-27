namespace Inu.Cate.I8080;

internal class WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    : Cate.WordLoadInstruction(function, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        if (SourceOperand is IndirectOperand || DestinationOperand is IndirectOperand) {
            if (DestinationOperand.Register is WordRegister wordRegister) {
                ViaRegister(wordRegister);
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(this, [WordRegister.De, WordRegister.Bc], SourceOperand);
                ViaRegister(reservation.WordRegister);
            }
            return;
        }
        base.BuildAssembly();
        return;

        void ViaRegister(Cate.WordRegister register)
        {
            register.Load(this, SourceOperand);
            register.Store(this, DestinationOperand);
        }
    }
}