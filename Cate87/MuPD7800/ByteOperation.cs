namespace Inu.Cate.MuCom87.MuPD7800
{
    internal class ByteOperation : MuCom87.ByteOperation
    {
        public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister,
            int offset, int value)
        {
            if (offset == 0) {
                instruction.WriteLine("\tmvix\t" + pointerRegister.AsmName + "," + value);
                return;
            }
            base.StoreConstantIndirect(instruction, pointerRegister, offset, value);
        }
    }
}
