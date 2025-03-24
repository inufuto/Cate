namespace Inu.Cate.I8086;

internal class ResizeInstruction(
    Function function,
    AssignableOperand destinationOperand,
    IntegerType destinationType,
    Operand sourceOperand,
    IntegerType sourceType)
    : Cate.ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType)
{
    protected override void ExpandSigned()
    {
        if (Equals(DestinationOperand.Register, WordRegister.Ax)) {
            ByteRegister.Al.Load(this, SourceOperand);
            WriteLine("\tcbw");
            WordRegister.Ax.Store(this, DestinationOperand);
            return;
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.Al)) {
            ByteRegister.Al.Load(this, SourceOperand);
            WriteLine("\tcbw");
            WordRegister.Ax.Store(this, DestinationOperand);
        }
    }

    protected override void Expand()
    {
        if (Equals(DestinationOperand.Register, WordRegister.Ax)) {
            ByteRegister.Al.Load(this, SourceOperand);
            //WriteLine("\tcbw");
            WriteLine("\txor ah,ah");
            WordRegister.Ax.Store(this, DestinationOperand);
            return;
        }
        if (SourceOperand.Register == null || Equals(SourceOperand.Register, ByteRegister.Al)) {
            using (ByteOperation.ReserveRegister(this, ByteRegister.Al)) {
                ByteRegister.Al.Load(this, SourceOperand);
                //WriteLine("\tcbw");
                WriteLine("\txor ah,ah");
                WordRegister.Ax.Store(this, DestinationOperand);
            }
            return;
        }
        base.Expand();
    }
}