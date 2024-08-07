namespace Inu.Cate.Z80;

internal class WordMonomialInstruction : MonomialInstruction
{
    public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

    public override void BuildAssembly()
    {
        void ViaRegister(Cate.WordRegister r)
        {
            using (var sourceReservation = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters, SourceOperand))
            {
                var sourceRegister = sourceReservation.WordRegister;
                sourceRegister.Load(this, SourceOperand);
                using (ByteOperation.ReserveRegister(this, ByteRegister.A))
                {
                    WriteLine("\tld\ta," + sourceRegister.Low);
                    WriteLine("\tcpl");
                    WriteLine("\tld\t" + r.Low + ",a");
                    WriteLine("\tld\ta," + sourceRegister.High);
                    WriteLine("\tcpl");
                    WriteLine("\tld\t" + r.High + ",a");
                }
            }

            if (OperatorId == '-')
            {
                WriteLine("\tinc\t" + r);
            }
        }

        if (DestinationOperand.Register is WordRegister wordRegister)
        {
            ViaRegister(wordRegister);
            return;
        }
        using var reserveAnyRegister = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters);
        ViaRegister(reserveAnyRegister.WordRegister);
        reserveAnyRegister.WordRegister.Store(this, DestinationOperand);
    }
}