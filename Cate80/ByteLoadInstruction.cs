namespace Inu.Cate.Z80;

internal class ByteLoadInstruction : Cate.ByteLoadInstruction
{
    public ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
        : base(function, destinationOperand, sourceOperand) { }

    public override void BuildAssembly()
    {
        if (SourceOperand is IntegerOperand sourceIntegerOperand) {
            if (DestinationOperand is IndirectOperand destinationIndirectOperand) {
                var pointer = destinationIndirectOperand.Variable;
                var offset = destinationIndirectOperand.Offset;
                var register = GetVariableRegister(pointer, 0, r => r is PointerRegister wr && wr.IsOffsetInRange(offset)) ??
                               GetVariableRegister(pointer, 0, r => r is PointerRegister wr && wr.IsOffsetInRange(0));
                {
                    if (register is Cate.PointerRegister pointerRegister) {
                        ByteOperation.StoreConstantIndirect(
                            this, pointerRegister, offset,
                            sourceIntegerOperand.IntegerValue
                        );
                        return;
                    }
                }
                using var reservation = PointerOperation.ReserveAnyRegister(this, PointerOperation.RegistersToOffset(offset));
                reservation.PointerRegister.LoadFromMemory(this, pointer, 0);
                ByteOperation.StoreConstantIndirect(
                    this, reservation.PointerRegister, offset,
                    sourceIntegerOperand.IntegerValue
                );
                return;
            }
        }
        base.BuildAssembly();
    }
}