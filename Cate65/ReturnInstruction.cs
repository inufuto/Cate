using System.Linq;

namespace Inu.Cate.Mos6502;

internal class ReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor)
    : Cate.ReturnInstruction(function, sourceOperand, anchor)
{
    public override void BuildAssembly()
    {
        LoadResult();
        if (!Equals(Function.Instructions.Last())) {
            WriteLine("\tjmp\t" + Anchor.Label);
        }
    }
}