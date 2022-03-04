namespace Inu.Cate.Tms99
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType) { }

        protected override void Expand()
        {
            Operate(false);
        }

        protected override void ExpandSigned()
        {
            Operate(true);
        }

        private void Operate(bool signed)
        {
            ByteOperation.UsingAnyRegister(this, DestinationOperand, SourceOperand, byteRegister =>
            {
                byteRegister.Load(this, SourceOperand);
                RemoveVariableRegister(byteRegister);
                var wordRegister = ((ByteRegister)byteRegister).Expand(this, signed);
                ChangedRegisters.Add(wordRegister);
                wordRegister.Store(this, DestinationOperand);
            });
        }

        protected override void Reduce()
        {
            WordOperation.UsingAnyRegister(this, DestinationOperand, SourceOperand, wordRegister =>
            {
                wordRegister.Load(this, SourceOperand);
                WriteLine("\tsla\t" + wordRegister + ",8");
                ((WordRegister)wordRegister).ByteRegister.Store(this, DestinationOperand);
                RemoveVariableRegister(wordRegister);
            });
        }

        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            return true;
        }
    }
}
