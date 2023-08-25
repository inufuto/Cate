namespace Inu.Cate.Sc62015
{
    internal class WordMonomialInstruction : MonomialInstruction
    {
        public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            switch (OperatorId) {
                case '~': {
                        void ViaRegister(WordInternalRam internalRam)
                        {
                            internalRam.Load(this, SourceOperand);
                            WriteLine("\txor (" + internalRam.Label + "),0ffh");
                            WriteLine("\txor (" + internalRam.Label + "+1),0ffh");
                            internalRam.Store(this, DestinationOperand);
                        }
                        if (DestinationOperand.Register is WordInternalRam wordInternalRam) {
                            ViaRegister(wordInternalRam);
                            return;
                        }
                        using var reservation = WordOperation.ReserveAnyRegister(this, WordInternalRam.Registers, SourceOperand);
                        ViaRegister((WordInternalRam)reservation.WordRegister);
                    }
                    break;
                case '-': {
                        void ViaRegister(Cate.WordRegister right)
                        {
                            using var reservation = WordOperation.ReserveAnyRegister(this, WordOperation.RegistersOtherThan(right));
                            var left = reservation.WordRegister;
                            right.Load(this, SourceOperand);
                            left.LoadConstant(this, 0);
                            WriteLine("\tsub " + left.AsmName + "," + right.AsmName);
                            left.Store(this, DestinationOperand);
                        }
                        if (DestinationOperand.Register is WordRegister wordRegister) {
                            ViaRegister(wordRegister);
                            return;
                        }
                        using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, SourceOperand);
                        ViaRegister(reservation.WordRegister);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
