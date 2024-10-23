﻿using System.Diagnostics;

namespace Inu.Cate.Sm83;

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
        if (IncrementOrDecrement())
            return;

        if (OperatorId == '+') {
            if (Equals(RightOperand.Register, WordRegister.Hl)) {
                using var reservation = PointerOperation.ReserveAnyRegister(this, new List<Cate.PointerRegister>() { PointerRegister.De, PointerRegister.Bc }, LeftOperand);
                var leftRegister = reservation.PointerRegister;
                leftRegister.Load(this, LeftOperand);
                WriteLine("\tadd\thl," + leftRegister.Name);
                AddChanged(PointerRegister.Hl);
                RemoveRegisterAssignment(PointerRegister.Hl);
                PointerRegister.Hl.Store(this, DestinationOperand);
                return;
            }
            if (Equals(LeftOperand.Register, WordRegister.Hl)) {
                using var reservation = PointerOperation.ReserveAnyRegister(this, new List<Cate.PointerRegister>() { PointerRegister.De, PointerRegister.Bc }, RightOperand);
                var rightRegister = reservation.PointerRegister;
                rightRegister.Load(this, RightOperand);
                WriteLine("\tadd\thl," + rightRegister.Name);
                AddChanged(PointerRegister.Hl);
                RemoveRegisterAssignment(PointerRegister.Hl);
                PointerRegister.Hl.Store(this, DestinationOperand);
                return;
            }
        }

        string lowOperation, highOperation;
        switch (OperatorId) {
            case '+':
                lowOperation = "add";
                highOperation = "adc";
                break;
            case '-':
                lowOperation = "sub";
                highOperation = "sbc";
                break;
            default:
                throw new NotImplementedException();
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
            ByteRegister.A.Operate(this, lowOperation, true, Compiler.LowByteOperand(RightOperand));
            ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
            ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
            ByteRegister.A.Operate(this, highOperation, true, Compiler.HighByteOperand(RightOperand));
            ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
        }
    }

    protected override int Threshold() => 8;

    protected override void Increment(int count)
    {
        IncrementOrDecrement("inc", count);
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("dec", count);
    }

    private void IncrementOrDecrement(string operation, int count)
    {
        void ViaRegister(Cate.PointerRegister r)
        {
            r.Load(this, LeftOperand);
            IncrementOrDecrement(this, operation, r, count);
        }

        if (DestinationOperand.Register is PointerRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers, LeftOperand);
        ViaRegister(reservation.PointerRegister);
        reservation.PointerRegister.Store(this, DestinationOperand);
    }

    private static void IncrementOrDecrement(Instruction instruction, string operation, Cate.PointerRegister leftRegister, int count)
    {
        Debug.Assert(count >= 0);
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + "\t" + leftRegister.Name);
        }
        instruction.RemoveRegisterAssignment(leftRegister);
        instruction.AddChanged(leftRegister);
    }
}