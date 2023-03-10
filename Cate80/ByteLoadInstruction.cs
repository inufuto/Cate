namespace Inu.Cate.Z80
{
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
                    var register = GetVariableRegister(pointer, 0, r => r is WordRegister wr && wr.IsPointer(offset));
                    {
                        if (register is WordRegister pointerRegister) {
                            ByteOperation.StoreConstantIndirect(
                                this, pointerRegister, offset,
                                sourceIntegerOperand.IntegerValue
                            );
                            return;
                        }
                    }
                    using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Pointers(offset));
                    reservation.WordRegister.LoadFromMemory(this, pointer, 0);
                    ByteOperation.StoreConstantIndirect(
                        this, reservation.WordRegister, offset,
                        sourceIntegerOperand.IntegerValue
                    );
                    return;
                }
            }
            base.BuildAssembly();
        }
    }
}