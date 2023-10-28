namespace Inu.Cate.Mc6800.Mc6801
{
    internal class ResizeInstruction : Mc6800.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType)
        { }

        protected override void Reduce()
        {
            using (WordOperation.ReserveRegister(this, PairRegister.D)) {
                PairRegister.D.Load(this, SourceOperand);
                ByteRegister.B.Store(this, DestinationOperand);
            }
            //base.Reduce();
        }

        protected override void Expand()
        {
            if (SourceOperand is VariableOperand variableOperand && Equals(GetVariableRegister(variableOperand), ByteRegister.B) && !IsRegisterReserved(ByteRegister.A)) {
                using (WordOperation.ReserveRegister(this, PairRegister.D)) {
                    ByteRegister.B.Load(this, SourceOperand);
                    WriteLine("\tclra");
                    PairRegister.D.Store(this, DestinationOperand);
                }
            }
            base.Expand();
        }
    }
}
