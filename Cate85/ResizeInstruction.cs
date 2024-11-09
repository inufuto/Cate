using System.Diagnostics;

namespace Inu.Cate.Sm85;

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
        using var reservation = WordOperation.ReserveAnyRegister(this, DestinationOperand);
        Debug.Assert(reservation.WordRegister.Low != null);
        reservation.WordRegister.Low.Load(this, SourceOperand);
        WriteLine("\texts\t" + reservation.WordRegister);
        reservation.WordRegister.Store(this, DestinationOperand);
    }
}