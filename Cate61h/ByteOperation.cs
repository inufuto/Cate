namespace Inu.Cate.Hd61700
{
    internal class ByteOperation : Cate.ByteOperation
    {
        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
        public override List<Cate.ByteRegister> Accumulators => Registers;

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
        {
            throw new NotImplementedException();
        }
    }
}
