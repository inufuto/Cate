namespace Inu.Cate.Mc6809
{
    internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
            : base(function, operand, anchor)
        { }

        public override void BuildAssembly()
        {
            ByteOperation.Operate(this, "dec", true, Operand, 1);
            WriteLine("\tbne\t" + Anchor.Label);
        }
    }
}