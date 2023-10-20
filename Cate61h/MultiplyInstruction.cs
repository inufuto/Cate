namespace Inu.Cate.Hd61700;

internal class MultiplyInstruction : Cate.MultiplyInstruction
{
    public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue) { }

    public override void BuildAssembly()
    {
        if (RightValue == 0) {
            void ViaRegister(Register r)
            {
                r.LoadConstant(this, 0);
            }

            if (DestinationOperand.Register is PointerRegister pointerRegister) {
                ViaRegister(pointerRegister);
                return;
            }
            using var reservation = PointerOperation.ReserveAnyRegister(this, WordPointerRegister.Registers, LeftOperand);
            ViaRegister(reservation.PointerRegister);
            reservation.WordRegister.Store(this, DestinationOperand);
            return;
        }
        if (BitCount == 1) {
            void ViaRegister(Cate.WordRegister r)
            {
                r.Load(this, LeftOperand);
                Shift(() => WriteLine("\tad " + r.AsmName + "," + r.AsmName));
                AddChanged(r);
                RemoveRegisterAssignment(r);
            }
            if (DestinationOperand.Register is WordRegister wordRegister) {
                ViaRegister(wordRegister);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, LeftOperand);
            ViaRegister(reservation.WordRegister);
            reservation.PointerRegister.Store(this, DestinationOperand);
            return;
        }
        {
            void ViaRegister(Register re)
            {
                using var additionReservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers);
                var additionRegister = additionReservation.WordRegister;
                additionRegister.Load(this, LeftOperand);
                re.LoadConstant(this, 0);
                Operate(
                    () =>
                    {
                        WriteLine("\tadw " + re.AsmName + "," + additionRegister.AsmName);
                    }, () =>
                    {
                        WriteLine("\tbiuw " + additionRegister.AsmName);
                    });
                AddChanged(re);
                RemoveRegisterAssignment(re);
                AddChanged(additionRegister);
                RemoveRegisterAssignment(additionRegister);
            }

            if (DestinationOperand.Register is WordRegister wordRegister) {
                ViaRegister(wordRegister);
                return;
            }
            using var addedReservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, LeftOperand);
            ViaRegister(addedReservation.WordRegister);
            addedReservation.WordRegister.Store(this, DestinationOperand);
        }
    }
}