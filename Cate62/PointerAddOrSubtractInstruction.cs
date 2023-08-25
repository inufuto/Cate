using System.Diagnostics;

namespace Inu.Cate.Sc62015
{
    internal class PointerAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public PointerAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            if (LeftOperand.Type is not PointerType) {
                Debug.Assert(RightOperand.Type is PointerType);
                ExchangeOperands();
            }
            if (IncrementOrDecrement())
                return;

            var operation = OperatorId switch
            {
                '+' => "add",
                '-' => "sub",
                _ => throw new NotImplementedException()
            };

            void ViaRegister(Cate.PointerRegister leftRegister)
            {
                leftRegister.Load(this, LeftOperand);
                leftRegister.Operate(this, operation, true, RightOperand);
                leftRegister.Store(this, DestinationOperand);
            }

            {
                if (DestinationOperand.Register is PointerRegister destinationRegister and not PointerInternalRam) {
                    using (PointerOperation.ReserveRegister(this, destinationRegister, LeftOperand)) {
                        ViaRegister(destinationRegister);
                    }
                }
                else {
                    using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers, LeftOperand);
                    ViaRegister(reservation.PointerRegister);
                }
            }
        }

        protected override int Threshold() => 3;

        protected override void Increment(int count)
        {
            IncrementOrDecrement("inc", count);
        }

        protected override void Decrement(int count)
        {
            IncrementOrDecrement("dec", count);
        }

        private void IncrementOrDecrement(string operation, int count)
        {
            if (DestinationOperand.Register is PointerRegister destinationRegister && destinationRegister is not PointerInternalRam) {
                destinationRegister.Load(this, LeftOperand);
                IncrementOrDecrement(this, operation, destinationRegister, count);
                return;
            }
            using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers, LeftOperand);
            reservation.PointerRegister.Load(this, LeftOperand);
            IncrementOrDecrement(this, operation, reservation.PointerRegister, count);
            reservation.PointerRegister.Store(this, DestinationOperand);
            ResultFlags |= Flag.Z;
        }

        private static void IncrementOrDecrement(Instruction instruction, string operation, Register leftRegister, int count)
        {
            Debug.Assert(count >= 0);
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + leftRegister);
            }
            instruction.RemoveRegisterAssignment(leftRegister);
            instruction.AddChanged(leftRegister);
        }
    }
}
