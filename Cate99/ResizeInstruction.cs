namespace Inu.Cate.Tms99
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType) { }

        protected override void Expand()
        {
            if (SourceOperand.Register is ByteRegister sourceRegister) {
                sourceRegister.WordRegister.Store(this, DestinationOperand);
                return;
            }
            ByteOperation.UsingAnyRegister(this, DestinationOperand, SourceOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, SourceOperand);
                ((ByteRegister)temporaryRegister).WordRegister.Store(this, DestinationOperand);
            });
        }

        protected override void Reduce()
        {
            WordOperation.UsingAnyRegister(this, DestinationOperand, SourceOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, SourceOperand);
                ((WordRegister)temporaryRegister).ByteRegister.Store(this, DestinationOperand);
            });
        }

        protected override void ExpandSigned()
        {
            var byteRegister = ByteRegister.FromIndex(0);
            var wordRegister = WordRegister.FromIndex(1);
            WordOperation.UsingRegister(this, wordRegister, () =>
            {
                byteRegister.Load(this, SourceOperand);
                Compiler.CallExternal(this, "cate.ExpandSigned");
                wordRegister.Store(this, DestinationOperand);
                RemoveVariableRegister(wordRegister);
                ChangedRegisters.Add(wordRegister);
            });
        }
    }
}
