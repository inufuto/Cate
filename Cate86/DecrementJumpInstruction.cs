namespace Inu.Cate.I8086
{
    internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor)
        { }

        public override void BuildAssembly()
        {
            if (Equals(Operand.Register, WordRegister.Cx)) {
                WriteLine("\tloop " + Anchor.Label);
                ChangedRegisters.Add(WordRegister.Cx);
                RemoveRegisterAssignment(WordRegister.Cx);
                return;
            }
            ByteOperation.Operate(this, "dec\t", true, Operand);
            WriteJumpLine("\tjnz " + Anchor.Label);
        }
    }
}
