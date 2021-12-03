namespace Inu.Cate.Mc6800
{
    internal class WordNegateInstruction : MonomialInstruction
    {
        public WordNegateInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand)
            : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            ByteRegister.UsingPair(this, () =>
            {
                ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                ByteRegister.B.Load(this, Compiler.LowByteOperand(SourceOperand));
                WriteLine("\tcomb");
                WriteLine("\tcoma");
                WriteLine("\taddb\t#1");
                WriteLine("\tadca\t#0");
                ByteRegister.A.Store(this,  Compiler.HighByteOperand(DestinationOperand));
                ByteRegister.B.Store(this,  Compiler.LowByteOperand(DestinationOperand));
            });
        }
    }
}