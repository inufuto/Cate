namespace Inu.Cate.Z80
{
    class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor) { }

        public override void BuildAssembly()
        {
            if (Equals(Operand.Register, ByteRegister.B)) {
                WriteLine("\tdjnz\t" + Anchor.Label);
                ChangedRegisters.Add(ByteRegister.B);
                RemoveRegisterAssignment(ByteRegister.B);
                return;
            }
            ByteOperation.Operate(this, "dec\t", true, Operand);
            WriteJumpLine("\tjr\tnz," + Anchor.Label);
        }
    }
}
