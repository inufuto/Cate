namespace Inu.Cate;

public abstract class DecrementJumpInstruction : Instruction
{
    public readonly AssignableOperand Operand;
    public readonly Anchor Anchor;

    protected DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function)
    {
        Operand = operand;
        Anchor = anchor;
        Anchor.AddOriginAddress(function.NextAddress);
        Operand.AddUsage(function.NextAddress, Variable.Usage.Read | Variable.Usage.Write);
    }

    public override string ToString() => "if --" + Operand + " != 0 goto " + Anchor.Label;


    public override bool IsJump() => true;

    public override void ReserveOperandRegisters()
    {
        ReserveOperandRegister(Operand);
    }

    public override bool IsSourceOperand(Variable variable)
    {
        return Operand.IsVariable(variable);
    }

    //public override void RemoveDestinationRegister()
    //{
    //    RemoveChangedRegisters(Operand);
    //}
}