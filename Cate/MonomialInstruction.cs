﻿using Inu.Language;

namespace Inu.Cate;

public abstract class MonomialInstruction : Instruction
{
    public readonly int OperatorId;
    public readonly AssignableOperand DestinationOperand;
    public readonly Operand SourceOperand;

    protected MonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand sourceOperand) : base(function)
    {
        OperatorId = operatorId;
        DestinationOperand = destinationOperand;
        SourceOperand = sourceOperand;

        DestinationOperand.AddUsage(function.NextAddress, Variable.Usage.Write);
        SourceOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
    }
    public override Operand? ResultOperand => DestinationOperand;

    public override void ReserveOperandRegisters()
    {
        ReserveOperandRegister(SourceOperand);
        if (DestinationOperand is IndirectOperand indirectOperand) {
            ReserveOperandRegister(indirectOperand);
        }
    }

    public override bool IsSourceOperand(Variable variable)
    {
        return SourceOperand.IsVariable(variable);
    }

    //public override void RemoveDestinationRegister()
    //{
    //    RemoveChangedRegisters(DestinationOperand);
    //}

    public override string ToString()
    {
        return DestinationOperand + " = " + ReservedWord.FromId(OperatorId) + " " + SourceOperand;
    }
}