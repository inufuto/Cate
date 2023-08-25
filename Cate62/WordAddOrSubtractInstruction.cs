using System.Diagnostics;

namespace Inu.Cate.Sc62015
{
    internal class WordAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public WordAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            if (LeftOperand is ConstantOperand && RightOperand is not ConstantOperand && IsOperatorExchangeable()) {
                ExchangeOperands();
            }
            else {
                if (RightOperand.Register is WordRegister) {
                    if (IsOperatorExchangeable()) {
                        ExchangeOperands();
                    }
                }
            }

            if (IncrementOrDecrement())
                return;

            var operation = OperatorId switch
            {
                '+' => "add",
                '-' => "sub",
                _ => throw new NotImplementedException()
            };

            void ViaRegister(Cate.WordRegister leftRegister)
            {
                leftRegister.Load(this, LeftOperand);
                if (RightOperand is ConstantOperand) {
                    using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, RightOperand);
                    reservation.WordRegister.Load(this, RightOperand);
                    WriteLine("\t" + operation + " " + leftRegister.AsmName + "," + reservation.WordRegister.AsmName);
                }
                else {
                    leftRegister.Operate(this, operation, true, RightOperand);
                }
                leftRegister.Store(this, DestinationOperand);
            }
            {
                if (DestinationOperand.Register is WordRegister destinationRegister and not WordInternalRam) {
                    using (WordOperation.ReserveRegister(this, destinationRegister, LeftOperand)) {
                        ViaRegister(destinationRegister);
                    }
                }
                else {
                    using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, LeftOperand);
                    ViaRegister(reservation.WordRegister);
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
            if (DestinationOperand.Register is WordRegister destinationRegister && destinationRegister is not WordInternalRam) {
                destinationRegister.Load(this, LeftOperand);
                IncrementOrDecrement(this, operation, destinationRegister, count);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, LeftOperand);
            reservation.WordRegister.Load(this, LeftOperand);
            IncrementOrDecrement(this, operation, reservation.WordRegister, count);
            reservation.WordRegister.Store(this, DestinationOperand);
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
