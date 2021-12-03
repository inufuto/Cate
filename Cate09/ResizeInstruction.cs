namespace Inu.Cate.Mc6809
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType)
        { }

        protected override void ExpandSigned()
        {
            WordOperation.UsingRegister(this, WordRegister.D, () =>
            {
                ByteRegister.B.Load(this, SourceOperand);
                WriteLine("\tsex");
                WordRegister.D.Store(this, DestinationOperand);
            });
        }

        protected override void ClearHighByte(Cate.ByteRegister register, Operand operand)
        {
            ByteOperation.Operate(this, "clr", true, operand);
        }
    }
}