namespace Inu.Cate.Sm83;

internal class JumpInstruction(Function function, Anchor anchor) : Inu.Cate.JumpInstruction(function, anchor)
{
    public override void BuildAssembly()
    {
        if (Anchor.Address != Address + 1) {
            WriteLine("\tjr\t" + Anchor);
        }
    }
}