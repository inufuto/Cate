namespace Inu.Cate.Sm83;

internal class DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
    : Cate.DecrementJumpInstruction(function, operand, anchor)
{
    public override void BuildAssembly()
    {
        ByteOperation.Operate(this, "dec\t", true, Operand);
        WriteJumpLine("\tjr\tnz," + Anchor.Label);
    }
}