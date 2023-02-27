using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand,
            int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                using var reservation = WordOperation.ReserveAnyRegister(this, WordOperation.Registers, DestinationOperand, LeftOperand);
                reservation.WordRegister.LoadConstant(this, 0);
                reservation.WordRegister.Store(this, DestinationOperand);
                return;
            }
            if (BitCount == 1) {
                using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.AddableRegisters, DestinationOperand, LeftOperand);
                reservation.WordRegister.Load(this, LeftOperand);
                Shift(() => WriteLine("\tadd\t" + reservation.WordRegister.Name + "," + reservation.WordRegister.Name));
                AddChanged(reservation.WordRegister);
                RemoveRegisterAssignment(reservation.WordRegister);
                reservation.WordRegister.Store(this, DestinationOperand);
                return;
            }

            using var addedReservation = WordOperation.ReserveAnyRegister(this, WordRegister.AddableRegisters,
                DestinationOperand, LeftOperand);
            var candidates = WordRegister.Registers
                .Where(r => !Equals(r, addedReservation.WordRegister) && !r.IsAddable()).ToList();
            using (var additionReservation = WordOperation.ReserveAnyRegister(this, candidates)) {
                additionReservation.WordRegister.Load(this, LeftOperand);
                addedReservation.WordRegister.LoadConstant(this, 0.ToString());
                Operate(
                    () =>
                    {
                        WriteLine("\tadd\t" + addedReservation.WordRegister.Name + "," +
                                  additionReservation.WordRegister.Name);
                    }, () =>
                    {
                        Debug.Assert(additionReservation.WordRegister.Low != null &&
                                     additionReservation.WordRegister.High != null);
                        WriteLine("\tsla\t" + additionReservation.WordRegister.Low.Name);
                        WriteLine("\trl\t" + additionReservation.WordRegister.High.Name);
                    });
                AddChanged(addedReservation.WordRegister);
                RemoveRegisterAssignment(addedReservation.WordRegister);
                AddChanged(additionReservation.WordRegister);
                RemoveRegisterAssignment(additionReservation.WordRegister);
            }
            addedReservation.WordRegister.Store(this, DestinationOperand);
        }
    }
}