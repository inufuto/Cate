namespace Inu.Cate.I8080
{
    internal class ByteMonomialInstruction : MonomialInstruction
    {
        public ByteMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            void Operate()
            {
                WriteLine("\tcma");
                if (OperatorId == '-') {
                    WriteLine("\tinr\ta");
                }
            }

            if (Equals(SourceOperand.Register, ByteRegister.A)) {
                Operate();
                ByteRegister.A.Store(this, DestinationOperand);
                return;
            }

            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, SourceOperand);
                Operate();
                ByteRegister.A.Store(this, DestinationOperand);
            });
        }
    }
}
