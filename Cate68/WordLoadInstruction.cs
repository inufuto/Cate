using Inu.Cate.Mc6800.Mc6801;

namespace Inu.Cate.Mc6800;

internal class WordLoadInstruction : LoadInstruction
{
    public WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
        : base(function, destinationOperand, sourceOperand) { }

    public override void BuildAssembly()
    {
        if (SourceOperand.SameStorage(DestinationOperand))
            return;

        if (DestinationOperand is IndirectOperand && SourceOperand is IndirectOperand) {
            using (WordOperation.ReserveRegister(this, PairRegister.D)) {
                ViaRegister(PairRegister.D);
            }
            return;
        }

        using var reservation = WordOperation.ReserveAnyRegister(this, SourceOperand);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(WordRegister wordRegister)
        {
            wordRegister.Load(this, SourceOperand);
            RemoveVariableRegister(DestinationOperand);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}