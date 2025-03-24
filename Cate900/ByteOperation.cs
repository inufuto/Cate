namespace Inu.Cate.Tlcs900;

internal class ByteOperation : Cate.ByteOperation
{
    public override List<Cate.ByteRegister> Registers => ByteRegister.All.Cast<Cate.ByteRegister>().ToList();
    public override List<Cate.ByteRegister> Accumulators => [ByteRegister.A];
    public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset, int value)
    {
        var pointerName = WordRegister.PointerName(pointerRegister);
        if (offset == 0) {
            instruction.WriteLine("\tld (" + pointerName + ")," + value);
        }
        else {
            instruction.WriteLine("\tld (" + pointerName + "+" + offset + ")," + value);
        }
    }

    public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
    {
        throw new NotImplementedException();
    }
}