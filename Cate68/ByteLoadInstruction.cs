namespace Inu.Cate.Mc6800
{
    internal class ByteLoadInstruction : Cate.ByteLoadInstruction
    {
        public ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
            : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (SourceOperand is IntegerOperand { IntegerValue: 0 }) {
                ByteOperation.Operate(this, "clr", true, DestinationOperand);
                return;
            }
            base.BuildAssembly();
        }
    }
}
