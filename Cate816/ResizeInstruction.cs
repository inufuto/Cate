namespace Inu.Cate.Wdc65816;

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
        if (Equals(DestinationOperand.Register, WordRegister.A)) {
            ViaA();
            return;
        }
        using (WordOperation.ReserveRegister(this, WordRegister.A)) {
            ViaA();
            WordRegister.A.Store(this, DestinationOperand);
        }
        return;

        void ViaA()
        {
            ByteRegister.A.Load(this, SourceOperand);
            Compiler.CallExternal(this, "Cate.ExpandSigned");
        }
    }
}