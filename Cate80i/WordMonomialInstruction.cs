namespace Inu.Cate.I8080
{
    internal class WordMonomialInstruction : MonomialInstruction
    {
        public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            void ViaRegister(Cate.WordRegister r)
            {
                r.Load(this, SourceOperand);
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    WriteLine("\tmov\ta," + r.Low);
                    WriteLine("\tcma");
                    WriteLine("\tmov\t" + r.Low + ",a");
                    WriteLine("\tmov\ta," + r.High);
                    WriteLine("\tcma");
                    WriteLine("\tmov\t" + r.High + ",a");
                }

                if (OperatorId == '-') {
                    WriteLine("\tinx\t" + r);
                }
            }

            if (DestinationOperand.Register is WordRegister destinationRegister) {
                ViaRegister(destinationRegister);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, SourceOperand);
            var wordRegister = reservation.WordRegister;
            ViaRegister(wordRegister);
            wordRegister.Store(this, DestinationOperand);
        }
    }
}
