namespace Inu.Cate.Sm85;

internal class MultiplyInstruction(
    Function function,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    int rightValue)
    : Cate.MultiplyInstruction(function, destinationOperand, leftOperand, rightValue)
{
    public override void BuildAssembly()
    {
        using var reservation = WordOperation.ReserveAnyRegister(this, DestinationOperand);
        reservation.WordRegister.Load(this, LeftOperand);
        WriteLine("\tmult\t" + reservation.WordRegister + "," + RightValue);
        reservation.WordRegister.Store(this, DestinationOperand);
    }
}