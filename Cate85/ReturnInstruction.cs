namespace Inu.Cate.Sm85;

internal class ReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor)
    : Cate.ReturnInstruction(function, sourceOperand, anchor)
{
    public override void BuildAssembly()
    {
        LoadResult();
        if (!Equals(Function.Instructions.Last())) {
            WriteLine("\tbr\tt," + Anchor.Label);
        }
    }
}