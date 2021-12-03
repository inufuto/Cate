namespace Inu.Cate.Mc6800
{
    internal class WordComplementInstruction : MonomialInstruction
    {
        public WordComplementInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand)
            : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (SourceOperand.SameStorage(DestinationOperand)) {
                ByteOperation.Operate(this, "com", true, Compiler.LowByteOperand(DestinationOperand));
                ByteOperation.Operate(this, "com", true, Compiler.HighByteOperand(DestinationOperand));
                return;
            }

            ByteOperation.UsingAnyRegister(this, register =>
            {
                register.Load(this, Compiler.LowByteOperand(SourceOperand));
                WriteLine("\tcom" + register);
                register.Store(this, Compiler.LowByteOperand(DestinationOperand));

                register.Load(this, Compiler.HighByteOperand(SourceOperand));
                WriteLine("\tcom" + register);
                register.Store(this, Compiler.HighByteOperand(DestinationOperand));
            });
        }
    }
}