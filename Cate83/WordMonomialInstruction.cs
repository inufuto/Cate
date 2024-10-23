namespace Inu.Cate.Sm83;

internal class WordMonomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand sourceOperand)
    : MonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        void ViaRegister(Cate.WordRegister r)
        {
            using (var sourceReservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, SourceOperand)) {
                var sourceRegister = sourceReservation.WordRegister;
                sourceRegister.Load(this, SourceOperand);
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    WriteLine("\tld\ta," + sourceRegister.Low);
                    WriteLine("\tcpl");
                    WriteLine("\tld\t" + r.Low + ",a");
                    WriteLine("\tld\ta," + sourceRegister.High);
                    WriteLine("\tcpl");
                    WriteLine("\tld\t" + r.High + ",a");
                }
            }

            if (OperatorId == '-') {
                WriteLine("\tinc\t" + r);
            }
        }

        if (DestinationOperand.Register is WordRegister wordRegister) {
            ViaRegister(wordRegister);
            return;
        }
        using var reserveAnyRegister = WordOperation.ReserveAnyRegister(this, WordRegister.Registers);
        ViaRegister(reserveAnyRegister.WordRegister);
        reserveAnyRegister.WordRegister.Store(this, DestinationOperand);
    }
}