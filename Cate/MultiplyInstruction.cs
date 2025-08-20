﻿using System;
using System.Collections.Generic;

namespace Inu.Cate;

public abstract class MultiplyInstruction : Instruction
{
    public readonly AssignableOperand DestinationOperand;
    public readonly Operand LeftOperand;
    public readonly int RightValue;

    protected MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand,
        int rightValue) : base(function)
    {
        DestinationOperand = destinationOperand;
        LeftOperand = leftOperand;
        RightValue = rightValue;

        DestinationOperand.AddUsage(function.NextAddress, Variable.Usage.Write);
        LeftOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
    }

    protected void Shift(Action action)
    {
        var mask = RightValue;
        while ((mask >>= 1) != 0) {
            action();
        }
    }

    protected void Operate(Action add, Action shift)
    {
        var mask = RightValue;
        while (true) {
            if ((mask & 1) != 0) {
                add();
            }
            mask >>= 1;
            if (mask != 0) {
                shift();
            }
            else {
                break;
            }
        }
    }

    public override Operand? ResultOperand => DestinationOperand;

    public override void ReserveOperandRegisters()
    {
        ReserveOperandRegister(LeftOperand);
        if (DestinationOperand is IndirectOperand indirectOperand) {
            ReserveOperandRegister(indirectOperand);
        }
    }

    public override bool IsSourceOperand(Variable variable)
    {
        return LeftOperand.IsVariable(variable);
    }

    public override List<Operand> SourceOperands => [LeftOperand];

    public override string ToString() => DestinationOperand + " = " + LeftOperand + " * " + RightValue;

    public int BitCount
    {
        get
        {
            var count = 0;
            var mask = 1;
            while (mask < 0x10000) {
                if ((RightValue & mask) != 0) {
                    ++count;
                }

                mask <<= 1;
            }
            return count;
        }
    }
}