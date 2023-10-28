namespace Inu.Cate.Mc6800.Mc6801;

internal class WordLoadInstruction : Cate.LoadInstruction
{
    public WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

    public override void BuildAssembly()
    {
        if (SourceOperand.SameStorage(DestinationOperand))
            return;

        if (SourceOperand is IndirectOperand || DestinationOperand is IndirectOperand) {
            ViaRegister(PairRegister.D);
            return;
        }

        using var reservation = WordOperation.ReserveAnyRegister(this, SourceOperand);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(WordRegister wordRegister)
        {
            wordRegister.Load(this, SourceOperand);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}