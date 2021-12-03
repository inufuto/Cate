namespace Inu.Cate.Mc6800
{
    internal class WordLoadInstruction : LoadInstruction
    {
        public WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
            : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (SourceOperand.SameStorage(DestinationOperand))
                return;

            if (DestinationOperand is IndirectOperand && SourceOperand is IndirectOperand) {
                ByteRegister.UsingPair(this, () =>
                {
                    ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                    ByteRegister.B.Load(this, Compiler.LowByteOperand(SourceOperand));
                    ByteRegister.A.Store(this,  Compiler.HighByteOperand(DestinationOperand));
                    ByteRegister.B.Store(this,  Compiler.LowByteOperand(DestinationOperand));
                });
                RemoveVariableRegister(DestinationOperand);
                return;
            }
            WordRegister.X.Load(this,  SourceOperand);
            WordRegister.X.Store(this,  DestinationOperand);
        }
    }
}