namespace Inu.Cate.Tms99
{
    internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor) { }

        public override void BuildAssembly()
        {
            Tms99.WordOperation.Operate(this, "dec", Operand);
            WriteLine("\tjne\t" + Anchor.Label);
        }
    }
}
