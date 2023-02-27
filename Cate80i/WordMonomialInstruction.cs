namespace Inu.Cate.I8080
{
    internal class WordMonomialInstruction : MonomialInstruction
    {
        public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, DestinationOperand, SourceOperand);
            var wordRegister = reservation.WordRegister;
            wordRegister.Load(this, SourceOperand);
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                WriteLine("\tmov\ta," + wordRegister.Low);
                WriteLine("\tcma");
                WriteLine("\tmov\t" + wordRegister.Low + ",a");
                WriteLine("\tmov\ta," + wordRegister.High);
                WriteLine("\tcma");
                WriteLine("\tmov\t" + wordRegister.High + ",a");
            }
            if (OperatorId == '-') {
                WriteLine("\tinx\t" + wordRegister);
            }
            wordRegister.Store(this, DestinationOperand);
        }
    }
}
