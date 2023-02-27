namespace Inu.Cate.Z80
{
    internal class WordMonomialInstruction : MonomialInstruction
    {
        public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            using var reserveAnyRegister = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters, DestinationOperand, null);
            var destinationRegister = reserveAnyRegister.WordRegister;
            using (var sourceReservation = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters, null, SourceOperand)) {
                var sourceRegister = sourceReservation.WordRegister;
                sourceRegister.Load(this, SourceOperand);
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    WriteLine("\tld\ta," + sourceRegister.Low);
                    WriteLine("\tcpl");
                    WriteLine("\tld\t" + destinationRegister.Low + ",a");
                    WriteLine("\tld\ta," + sourceRegister.High);
                    WriteLine("\tcpl");
                    WriteLine("\tld\t" + destinationRegister.High + ",a");
                }
            }
            if (OperatorId == '-') {
                WriteLine("\tinc\t" + destinationRegister);
            }
            destinationRegister.Store(this, DestinationOperand);
        }
    }
}