namespace Inu.Cate.Mos6502
{
    internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor)
        { }

        public override void BuildAssembly()
        {
            if (Operand.Register is ByteRegister register) {
                register.Decrement(this);
            }
            else {
                ByteOperation.Operate(this, "dec", true, Operand);
            }
            WriteLine("\tbne\t" + Anchor.Label);
        }
    }
}