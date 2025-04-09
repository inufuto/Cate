namespace Inu.Cate.Z80;

class DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
    : Cate.DecrementJumpInstruction(function, operand, anchor)
{
    public override void BuildAssembly()
    {
        if (Equals(Operand.Register, ByteRegister.B)) {
            WriteLine("\tdjnz\t" + Anchor.Label);
            AddChanged(ByteRegister.B);
            RemoveRegisterAssignment(ByteRegister.B);
            return;
        }
        ByteOperation.Operate(this, "dec\t", true, Operand);
        WriteJumpLine("\tjr\tnz," + Anchor.Label);
    }
}