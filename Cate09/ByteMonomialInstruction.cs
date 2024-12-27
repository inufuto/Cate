using System;

namespace Inu.Cate.Mc6809;

internal class ByteMonomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand sourceOperand)
    : MonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        var operation = OperatorId switch
        {
            '-' => "neg",
            '~' => "com",
            _ => throw new NotImplementedException()
        };
        if (DestinationOperand.Register is ByteRegister byteRegister) {
            ViaRegister(byteRegister);
            return;
        }
        if (DestinationOperand.SameStorage(SourceOperand)) {
            ByteOperation.Operate(this, operation, true, DestinationOperand);
            return;
        }
        using var reservation = ByteOperation.ReserveAnyRegister(this, SourceOperand);
        ViaRegister(reservation.ByteRegister);
        reservation.ByteRegister.Store(this, DestinationOperand);
        return;

        void ViaRegister(Cate.ByteRegister register)
        {
            register.Load(this, SourceOperand);
            register.Operate(this, operation, true, 1);
        }
    }
}