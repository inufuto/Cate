using Microsoft.Win32;

namespace Inu.Cate.Hd61700;

internal class PointerAddOrSubtractInstruction : AddOrSubtractInstruction
{
    public PointerAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

    public override void BuildAssembly()
    {
        var operation = OperatorId switch
        {
            '+' => "adw",
            '-' => "sbw",
            _ => throw new NotImplementedException()
        };
        if (DestinationOperand.Register is PointerRegister destinationRegister && !Equals(RightOperand.Register, destinationRegister)) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = PointerOperation.ReserveAnyRegister(this, LeftOperand);
        {
            ViaRegister(reservation.PointerRegister);
        }
        return;

        void ViaRegister(PointerRegister register)
        {
            register.Load(this, LeftOperand);
            register.Operate(this, operation, true, RightOperand);
            register.Store(this, DestinationOperand);
        }
    }

    protected override int Threshold()
    {
        throw new NotImplementedException();
    }

    protected override void Increment(int count)
    {
        throw new NotImplementedException();
    }

    protected override void Decrement(int count)
    {
        throw new NotImplementedException();
    }
}