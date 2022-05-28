namespace Inu.Cate.I8086
{
    internal class ByteLoadInstruction : Cate.ByteLoadInstruction
    {
        public ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand)
        { }

        public override void BuildAssembly()
        {
            if (
                DestinationOperand is VariableOperand { Register: null } destinationVariableOperand
            ) {
                if (SourceOperand is IntegerOperand integerOperand) {
                    WriteLine("\tmov byte ptr [" + destinationVariableOperand.MemoryAddress() + "]," + integerOperand.IntegerValue);
                    return;
                }
            }
            if (DestinationOperand is IndirectOperand destinationIndirectOperand && destinationIndirectOperand.Variable.Register is WordRegister wordRegister && wordRegister.IsPointer(destinationIndirectOperand.Offset)) {
                if (SourceOperand is IntegerOperand integerOperand) {
                    var offset = destinationIndirectOperand.Offset;
                    var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
                    WriteLine("\tmov byte ptr [" + WordRegister.AsPointer(wordRegister) + addition + "]," + integerOperand.IntegerValue);
                    return;
                }
            }
            base.BuildAssembly();
        }
    }
}
