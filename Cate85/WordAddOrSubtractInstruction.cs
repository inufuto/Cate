using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class WordAddOrSubtractInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (IsOperatorExchangeable()) {
            if (RightOperand.Register != null && LeftOperand.Register == null) {
                ExchangeOperands();
            }
        }
        if (IncrementOrDecrement()) return;

        var operation = OperatorId switch
        {
            '+' => "addw",
            '-' => "subw",
            _ => throw new NotImplementedException()
        };
        if (DestinationOperand.Register is WordRegister destinationRegister && !Equals(destinationRegister, RightOperand.Register)) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, LeftOperand);
        ViaRegister(reservation.WordRegister);
        reservation.WordRegister.Store(this, DestinationOperand);
        return;

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, LeftOperand);
            wordRegister.Operate(this, operation, true, RightOperand);
            RemoveRegisterAssignment(wordRegister);
            AddChanged(wordRegister);
        }
    }

    protected override int Threshold() => 2;

    protected override void Increment(int count)
    {
        IncrementOrDecrement("incw", count);
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("decw", count);
    }

    private void IncrementOrDecrement(string operation, int count)
    {
        if (DestinationOperand.Register is WordRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, LeftOperand);
        ViaRegister(reservation.WordRegister);
        reservation.WordRegister.Store(this, DestinationOperand);
        return;

        void ViaRegister(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, LeftOperand);
            IncrementOrDecrement(operation, wordRegister, count);
        }
    }

    private void IncrementOrDecrement(string operation, Cate.WordRegister wordRegister, int count)
    {
        Debug.Assert(count >= 0);
        for (var i = 0; i < count; ++i) {
            WriteLine("\t" + operation + "\t" + wordRegister);
        }
        RemoveRegisterAssignment(wordRegister);
        AddChanged(wordRegister);
    }
}