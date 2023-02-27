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
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                    using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                        ByteRegister.B.Load(this, Compiler.LowByteOperand(SourceOperand));
                        ByteRegister.B.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    }
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                }
                RemoveVariableRegister(DestinationOperand);
                return;
            }
            WordRegister.X.Load(this, SourceOperand);
            WordRegister.X.Store(this, DestinationOperand);
        }
    }
}