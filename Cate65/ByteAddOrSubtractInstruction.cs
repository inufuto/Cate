using System;
using System.Collections.Generic;

namespace Inu.Cate.Mos6502;

internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
{
    public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
    { }

    public override void BuildAssembly()
    {
        if (Equals(RightOperand.Register, ByteRegister.A) && !Equals(LeftOperand.Register, ByteRegister.A) && IsOperatorExchangeable()) {
            ExchangeOperands();
        }
        if (CanIncrementOrDecrement() && IncrementOrDecrement())
            return;

        var operation = OperatorId switch
        {
            '+' => "clc|adc",
            '-' => "sec|sbc",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Flag.Z;

        ByteOperation.OperateByteBinomial(this, operation, true);
    }

    protected virtual bool CanIncrementOrDecrement()
    {
        if (LeftOperand is IndirectOperand)
            return false;
        if (DestinationOperand is IndirectOperand)
            return false;
        if (Equals(LeftOperand.Register, ByteRegister.A))
            return false;
        if (Equals(DestinationOperand.Register, ByteRegister.A))
            return false;
        return true;
    }


    protected override int Threshold() => 1;
    protected override void Increment(int count)
    {
        IncrementOrDecrement("in", "inc", count);
        ResultFlags |= Flag.Z;
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("de", "dec", count);
        ResultFlags |= Flag.Z;
    }

    private void IncrementOrDecrement(string registerOperation, string memoryOperation, int count)
    {
        if (DestinationOperand.SameStorage(LeftOperand)) {
            OperateByte(memoryOperation, count);
            return;
        }

        void ViaRegister(Cate.ByteRegister r)
        {
            r.Load(this, LeftOperand);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + registerOperation + r);
            }

            RemoveRegisterAssignment(r);
            AddChanged(r);
        }

        if (DestinationOperand.Register is ByteRegister byteRegister && !RightOperand.Conflicts(byteRegister)) {
            ViaRegister(byteRegister);
            return;
        }
        var candidates = new List<Cate.ByteRegister>() { ByteRegister.X, ByteRegister.Y };
        using var reservation = ByteOperation.ReserveAnyRegister(this, candidates, LeftOperand);
        ViaRegister(reservation.ByteRegister);
        reservation.ByteRegister.Store(this, DestinationOperand);
    }
}