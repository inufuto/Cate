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
                if (IsOperatorExchangeable()) {
                    if (DestinationOperand.Type is PointerType) {
                        if (LeftOperand.Type is not PointerType) {
                            ExchangeOperands();
                        }
                    }
                    else if (
                        LeftOperand.Register is not (WordRegister or PointerRegister) &&
                        DestinationOperand.Register is not (WordRegister or PointerRegister) &&
                        RightOperand.Register is WordRegister or PointerRegister
                    ) {
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

            {
                if (DestinationOperand.Register is WordRegister wordRegister and not WordInternalRam && !wordRegister.Conflicts(RightOperand.Register)) {
                    using (WordOperation.ReserveRegister(this, wordRegister, LeftOperand)) {
                        ViaRegister(wordRegister);
                    }
                }
                else if (DestinationOperand.Register is PointerRegister pointerRegister and not PointerInternalRam && !pointerRegister.Conflicts(RightOperand.Register)) {
                    using (WordOperation.ReserveRegister(this, pointerRegister, LeftOperand)) {
                        ViaRegister(pointerRegister);
                    }
                }
                else {
                    var candidates = LeftOperand.Type.ByteCount == 3 ? PointerRegister.Registers : WordRegister.Registers;
                    using var reservation = WordOperation.ReserveAnyRegister(this, candidates, LeftOperand);
                    ViaRegister(reservation.WordRegister);
                }
            }
            return;

            void ViaRegister(Cate.WordRegister leftRegister)
            {
                leftRegister.Load(this, LeftOperand);
                if (RightOperand is ConstantOperand) {
                    using var reservation = WordOperation.ReserveAnyRegister(this, RightOperand);
                    reservation.WordRegister.Load(this, RightOperand);
                    WriteLine("\t" + operation + " " + leftRegister.AsmName + "," + reservation.WordRegister.AsmName);
                }
                else {
                    leftRegister.Operate(this, operation, true, RightOperand);
                }
                leftRegister.Store(this, DestinationOperand);
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
            if (DestinationOperand.Register is WordRegister wordRegister and not WordInternalRam) {
                wordRegister.Load(this, LeftOperand);
                IncrementOrDecrement(this, operation, wordRegister, count);
                return;
            }
            if (DestinationOperand.Register is PointerRegister pointerRegister and not PointerInternalRam) {
                pointerRegister.Load(this, LeftOperand);
                IncrementOrDecrement(this, operation, pointerRegister, count);
                return;
            }

            var candidates = LeftOperand.Type.ByteCount == 3 ? PointerRegister.Registers : WordRegister.Registers;
            using var reservation = WordOperation.ReserveAnyRegister(this, candidates, LeftOperand);
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
