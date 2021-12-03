namespace Inu.Cate.Z80
{
    class WordMonomialInstruction : MonomialInstruction
    {
        public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            WordRegister.UsingAny(this, WordRegister.PairRegisters, DestinationOperand, destinationRegister =>
            {
                WordRegister.UsingAny(this, WordRegister.PairRegisters, SourceOperand, sourceRegister =>
                {
                    sourceRegister.Load(this, SourceOperand);
                    ByteRegister.UsingAccumulator(this, () =>
                    {
                        WriteLine("\tld\ta," + sourceRegister.Low);
                        WriteLine("\tcpl");
                        WriteLine("\tld\t" + destinationRegister.Low + ",a");
                        WriteLine("\tld\ta," + sourceRegister.High);
                        WriteLine("\tcpl");
                        WriteLine("\tld\t" + destinationRegister.High + ",a");
                    });
                });
                if (OperatorId == '-') {
                    WriteLine("\tinc\t" + destinationRegister);
                }
                destinationRegister.Store(this, DestinationOperand);
            });
        }
    }
}