namespace Inu.Cate.I8086;

internal class JumpInstruction(Function function, Anchor anchor) : Cate.JumpInstruction(function, anchor)
{
    public override void BuildAssembly()
    {
        if (Anchor.Address != Address + 1) {
            WriteLine("\tjmp " + Anchor);
        }
    }
}