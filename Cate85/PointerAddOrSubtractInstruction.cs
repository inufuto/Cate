using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class PointerAddOrSubtractInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (LeftOperand.Type is not PointerType) {
            ExchangeOperands();
        }
        if (IncrementOrDecrement()) return;

        var operation = OperatorId switch
        {
            '+' => "addw",
            '-' => "subw",
            _ => throw new NotImplementedException()
        };
        if (DestinationOperand.Register is PointerRegister destinationRegister) {
            if (RightOperand.Register == null || !RightOperand.Register.Conflicts(DestinationOperand.Register)) {
                ViaRegister(destinationRegister);
                return;
            }
        }
        using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers, LeftOperand);
        ViaRegister(reservation.PointerRegister);
        reservation.PointerRegister.Store(this, DestinationOperand);
        return;

        void ViaRegister(Cate.PointerRegister pointerRegister)
        {
            Debug.Assert(pointerRegister.WordRegister != null);
            pointerRegister.Load(this, LeftOperand);
            pointerRegister.WordRegister.Operate(this, operation, true, RightOperand);
            RemoveRegisterAssignment(pointerRegister);
            AddChanged(pointerRegister);
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
        if (DestinationOperand.Register is PointerRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers, LeftOperand);
        ViaRegister(reservation.PointerRegister);
        reservation.PointerRegister.Store(this, DestinationOperand);
        return;

        void ViaRegister(Cate.PointerRegister pointerRegister)
        {
            pointerRegister.Load(this, LeftOperand);
            IncrementOrDecrement(operation, pointerRegister, count);
        }
    }

    private void IncrementOrDecrement(string operation, Cate.PointerRegister pointerRegister, int count)
    {
        Debug.Assert(count >= 0);
        for (var i = 0; i < count; ++i) {
            WriteLine("\t" + operation + "\t" + pointerRegister);
        }
        RemoveRegisterAssignment(pointerRegister);
        AddChanged(pointerRegister);
    }
}