namespace Inu.Cate.Sm85;

internal class ByteOperation : Cate.ByteOperation
{
    public override List<Cate.ByteRegister> Registers => ByteRegister.Registers.Union(ByteRegisterFile.Registers).ToList();
    public override List<Cate.ByteRegister> Accumulators => ByteRegister.Registers;
    public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
    {
        throw new NotImplementedException();
    }
}