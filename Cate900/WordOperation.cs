namespace Inu.Cate.Tlcs900;

internal class WordOperation : Cate.WordOperation
{
    public override List<Cate.WordRegister> Registers => WordRegister.All.Cast<Cate.WordRegister>().ToList();
    public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset, int value)
    {
        var pointerName = WordRegister.PointerName(pointerRegister);
        if (offset == 0) {
            instruction.WriteLine("\tldw (" + pointerName + ")," + value);
        }
        else {
            instruction.WriteLine("\tldw (" + pointerName + "+" + offset + ")," + value);
        }
    }
}