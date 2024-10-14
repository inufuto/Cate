namespace Inu.Cate.Sm83;

internal class ByteOperation : Cate.ByteOperation
{
    public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
    public override List<Cate.ByteRegister> Accumulators => ByteRegister.Accumulators;
    public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset, int value)
    {
        if (offset == 0) {
            if (Equals(pointerRegister, PointerRegister.Hl)) {
                instruction.WriteLine("\tld\t(" + pointerRegister + ")," + value);
                return;
            }
            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadConstant(instruction, value);
                instruction.WriteLine("\tld\t(" + pointerRegister + "),a");
            }
            return;
        }
        if (Equals(pointerRegister, PointerRegister.Hl)) {
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                StoreConstantIndirect(instruction, pointerRegister, 0, value);
            });
            return;
        }
        var candidates = new List<Cate.PointerRegister>(PointerRegister.Registers.Where(r => !Equals(r, pointerRegister)).ToList());
        using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
        reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
        StoreConstantIndirect(instruction, reservation.PointerRegister, offset, value);
    }

    public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
    {
        throw new NotImplementedException();
    }
}