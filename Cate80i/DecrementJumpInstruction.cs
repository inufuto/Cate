namespace Inu.Cate.I8080
{
    internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor) { }

        public override void BuildAssembly()
        {
            ByteOperation.Operate(this, "dcr\t", true, Operand);
            WriteJumpLine("\tjnz\t" + Anchor.Label);
        }
    }
}
