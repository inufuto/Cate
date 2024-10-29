namespace Inu.Cate.Sm85;

internal class WordBitInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : BinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        var operation = OperatorId switch
        {
            '|' => "orw",
            '^' => "xorw",
            '&' => "andw",
            _ => throw new NotImplementedException()
        };

        if (DestinationOperand.Register is WordRegister wordRegister) {
            ViaRegister(wordRegister);
            return;
        }

        using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(Cate.WordRegister register)
        {
            register.Load(this, LeftOperand);
            register.Operate(this, operation, true, RightOperand);
            register.Store(this, DestinationOperand);
        }
    }
}