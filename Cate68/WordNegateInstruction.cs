namespace Inu.Cate.Mc6800
{
    internal class WordNegateInstruction : MonomialInstruction
    {
        public WordNegateInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand)
            : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                    ByteRegister.B.Load(this, Compiler.LowByteOperand(SourceOperand));
                    WriteLine("\tcomb");
                    WriteLine("\tcoma");
                    WriteLine("\taddb\t#1");
                    WriteLine("\tadca\t#0");
                    ByteRegister.B.Store(this, Compiler.LowByteOperand(DestinationOperand));
                }
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            }
        }
    }
}