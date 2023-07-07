using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue) { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                void ViaRegister(Cate.WordRegister r)
                {
                    r.LoadConstant(this, 0);
                }

                if (DestinationOperand.Register is WordRegister wordRegister) {
                    ViaRegister(wordRegister);
                    return;
                }
                using var reservation = WordOperation.ReserveAnyRegister(this, WordOperation.Registers, LeftOperand);
                ViaRegister(reservation.WordRegister);
                reservation.WordRegister.Store(this, DestinationOperand);
                return;
            }
            if (BitCount == 1) {
                void ViaRegister(Cate.WordRegister r)
                {
                    r.Load(this, LeftOperand);
                    Shift(() => WriteLine("\tadd\t" + r.Name + "," + r.Name));
                    AddChanged(r);
                    RemoveRegisterAssignment(r);
                }
                if (DestinationOperand.Register is WordRegister wordRegister) {
                    ViaRegister(wordRegister);
                    return;
                }
                using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.AddableRegisters, LeftOperand);
                ViaRegister(reservation.WordRegister);
                reservation.WordRegister.Store(this, DestinationOperand);
                return;
            }
            {
                void ViaRegister(Cate.WordRegister re)
                {
                    var candidates = WordRegister.Registers
                        .Where(r => !Equals(r, re) && !((WordRegister)r).Addable).ToList();
                    using var additionReservation = WordOperation.ReserveAnyRegister(this, candidates);
                    var additionRegister = additionReservation.WordRegister;
                    additionRegister.Load(this, LeftOperand);
                    re.LoadConstant(this, 0.ToString());
                    Operate(
                        () =>
                        {
                            WriteLine("\tadd\t" + re.Name + "," +
                                      additionRegister.Name);
                        }, () =>
                        {
                            Debug.Assert(additionRegister is { Low: { }, High: { } });
                            WriteLine("\tsla\t" + additionRegister.Low.Name);
                            WriteLine("\trl\t" + additionRegister.High.Name);
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
                using var addedReservation = WordOperation.ReserveAnyRegister(this, WordRegister.AddableRegisters, LeftOperand);
                ViaRegister(addedReservation.WordRegister);
                addedReservation.WordRegister.Store(this, DestinationOperand);
            }
        }
    }
}