﻿using System;

namespace Inu.Cate;

public abstract class ShiftInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : BinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected abstract int Threshold();

    public override void BuildAssembly()
    {
        if (RightOperand is IntegerOperand integerIntegerOperand) {
            var count = Math.Min(integerIntegerOperand.IntegerValue, DestinationOperand.Type.ByteCount * 8);
            if (count <= Threshold()) {
                ShiftConstant(count);
                return;
            }
        }
        var counterOperand = RightOperand.Type.ByteCount == 1 ? RightOperand : WordOperation.LowByteOperand(RightOperand);
        ShiftVariable(counterOperand);
    }

    protected abstract void ShiftConstant(int count);
    protected abstract void ShiftVariable(Operand counterOperand);

}

public abstract class ByteShiftInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : ShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override int Threshold() => 8;

    protected override void ShiftConstant(int count)
    {
        var operation = Operation();
        OperateByte(operation, count);
    }

    protected abstract string Operation();
}

public abstract class WordShiftInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : ShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override int Threshold() => 8;
}