namespace Inu.Cate.Wdc65816;

internal class ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    : Cate.ByteLoadInstruction(function, destinationOperand, sourceOperand)
{
    protected override List<Cate.ByteRegister> Candidates()
    {
        if (SourceOperand is IndirectOperand || DestinationOperand is IndirectOperand) {
            return [ByteRegister.A];
        }
        return base.Candidates();
    }
}