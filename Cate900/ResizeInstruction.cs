using System.Diagnostics;

namespace Inu.Cate.Tlcs900;

internal class ResizeInstruction(
    Function function,
    AssignableOperand destinationOperand,
    IntegerType destinationType,
    Operand sourceOperand,
    IntegerType sourceType)
    : Cate.ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType)
{
    protected override void ExpandSigned()
    {
        Expand("exts");
    }

    protected override void Expand()
    {
        Expand("extz");
    }

    private void Expand(string operation)
    {
        if (DestinationOperand.Register is WordRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        if (SourceOperand.Register is ByteRegister sourceRegister) {
            Debug.Assert(sourceRegister.WordRegister != null);
            if (sourceRegister.WordRegister.High != null && !IsRegisterReserved(sourceRegister.WordRegister.High)) {
                ViaRegister(sourceRegister.WordRegister);
                return;
            }
        }
        using var reservation = WordOperation.ReserveAnyRegister(this);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            Debug.Assert(wordRegister.Low != null);
            wordRegister.Low.Load(this, SourceOperand);
            WriteLine("\t" + operation + " " + wordRegister);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}