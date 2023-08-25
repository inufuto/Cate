namespace Inu.Cate.I8080
{
    internal class ByteLoadInstruction : Cate.ByteLoadInstruction
    {
        public ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (SourceOperand is IntegerOperand sourceIntegerOperand) {
                if (DestinationOperand is IndirectOperand destinationIndirectOperand) {
                    var pointer = destinationIndirectOperand.Variable;
                    var offset = destinationIndirectOperand.Offset;
                    var register = GetVariableRegister(pointer, 0);
                    {
                        if (register is PointerRegister pointerRegister) {
                            ByteOperation.StoreConstantIndirect(
                                this, pointerRegister, offset,
                                sourceIntegerOperand.IntegerValue
                            );
                            return;
                        }
                    }
                    using (PointerOperation.ReserveRegister(this, PointerRegister.Hl)) {
                        PointerRegister.Hl.LoadFromMemory(this, pointer, 0);
                        ByteOperation.StoreConstantIndirect(this, PointerRegister.Hl, offset, sourceIntegerOperand.IntegerValue);
                    }
                    return;
                }
            }
            base.BuildAssembly();
        }
    }
}
