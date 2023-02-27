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
            using var reservation = ByteOperation.ReserveAnyRegister(this, DestinationOperand, SourceOperand);
            var byteRegister = reservation.ByteRegister;
            byteRegister.Load(this, SourceOperand);
            RemoveRegisterAssignment(byteRegister);
            var wordRegister = ((ByteRegister)byteRegister).Expand(this, signed);
            AddChanged(wordRegister);
            wordRegister.Store(this, DestinationOperand);
        }

        private void Reduce(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, SourceOperand);
            WriteLine("\tsla\t" + wordRegister + ",8");
            AddChanged(wordRegister);
            ((WordRegister)wordRegister).ByteRegister.Store(this, DestinationOperand);
            RemoveRegisterAssignment(wordRegister);
        }

        protected override void Reduce()
        {
            if (DestinationOperand.Register is ByteRegister byteRegister) {
                Reduce(byteRegister.WordRegister);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, DestinationOperand, SourceOperand);
            Reduce(reservation.WordRegister);
        }

        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            return true;
        }
    }
}
