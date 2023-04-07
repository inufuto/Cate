namespace Inu.Cate
{
    public abstract class JumpInstruction : Instruction
    {
        public readonly Anchor Anchor;

        protected JumpInstruction(Function function, Anchor anchor) : base(function)
        {
            Anchor = anchor;
            Anchor.AddOriginAddress(function.NextAddress);
        }

        public override string ToString() => "goto " + Anchor;

        public override bool IsJump() => true;

        public override bool IsUnconditionalJump() => true;

        public override void ReserveOperandRegisters() { }

        public override bool IsSourceOperand(Variable variable) => false;
        //public override void RemoveDestinationRegister() { }
    }
}