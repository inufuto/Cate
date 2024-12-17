namespace Inu.Cate.Wdc65816;

internal class MultiplyInstruction(
    Function function,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    int rightValue)
    : Cate.MultiplyInstruction(function, destinationOperand, leftOperand, rightValue)
{
    public override void BuildAssembly()
    {
        if (RightValue == 0) {
            using (WordOperation.ReserveRegister(this, WordRegister.A)) {
                WordRegister.A.LoadConstant(this, 0);
                WordRegister.A.Store(this, DestinationOperand);
                return;
            }
        }
        if (BitCount == 1) {
            if (Equals(DestinationOperand.Register, WordRegister.A)) {
                ViaA();
                return;
            }
            using (WordOperation.ReserveRegister(this, WordRegister.A)) {
                ViaA();
            }
            return;

            void ViaA()
            {
                WordRegister.A.Load(this, LeftOperand);
                WordRegister.A.MakeSize(this);
                Shift(() =>
                {
                    WriteLine("\tasl\ta");
                });
                WordRegister.A.Store(this, DestinationOperand);
            }
        }
        {
            if (Equals(DestinationOperand.Register, WordRegister.A)) {
                ViaA();
                return;
            }
            using (WordOperation.ReserveRegister(this, WordRegister.A)) {
                ViaA();
            }

            void ViaA()
            {
                using var reservation = WordOperation.ReserveAnyRegister(this, WordZeroPage.Registers);
                var zeroPage = reservation.WordRegister;
                zeroPage.Load(this, LeftOperand);
                WordRegister.A.LoadConstant(this, 0);
                Operate(() =>
                {
                    WordRegister.A.MakeSize(this);
                    WriteLine("\tclc|adc\t" + zeroPage);
                }, () =>
                {
                    WordRegister.A.MakeSize(this);
                    WriteLine("\tasl\t" + zeroPage);
                });
                WordRegister.A.Store(this, DestinationOperand);
            }
        }
    }
}