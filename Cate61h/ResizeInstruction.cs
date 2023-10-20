namespace Inu.Cate.Hd61700
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType) { }

        protected override void Reduce()
        {
            if (DestinationOperand.Register is ByteRegister byteRegister) {
                ViaRegister(byteRegister);
                return;
            }

            using (var reservation = ByteOperation.ReserveAnyRegister(this)) {
                ViaRegister(reservation.ByteRegister);
            }
            return;

            void ViaRegister(Cate.ByteRegister destinationRegister)
            {
                using var reservation = WordOperation.ReserveAnyRegister(this, SourceOperand);
                var sourceRegister = reservation.WordRegister;
                sourceRegister.Load(this, SourceOperand);
                WriteLine("\tld " + destinationRegister.AsmName + "," + sourceRegister.AsmName);
                destinationRegister.Store(this, DestinationOperand);
            }
        }

        protected override void ExpandSigned()
        {
            var sourceRegister = ByteRegister.Registers[0];
            using (ByteOperation.ReserveRegister(this, sourceRegister)) {
                sourceRegister.Load(this, SourceOperand);
                var destinationRegister = WordRegister.Registers[0];
                if (Equals(DestinationOperand.Register, destinationRegister)) {
                    Compiler.CallExternal(this, "cate.ExpandSigned");
                }
                else {
                    using (WordOperation.ReserveRegister(this, destinationRegister)) {
                        Compiler.CallExternal(this, "cate.ExpandSigned");
                        destinationRegister.Store(this, DestinationOperand);
                    }
                }
            }
        }

        protected override void Expand()
        {
            using var brr = ByteOperation.ReserveAnyRegister(this, SourceOperand);
            var byteRegister = brr.ByteRegister;
            byteRegister.Load(this, SourceOperand);
            using var wrr = WordOperation.ReserveAnyRegister(this, DestinationOperand);
            var wordRegister = (WordRegister)wrr.WordRegister;
            WriteLine("\tld " + wordRegister.AsmName + "," + byteRegister.AsmName);
            WriteLine("\tld " + wordRegister.HighByteName + ",$sx");
            wordRegister.Store(this, DestinationOperand);
        }

        public override bool CanAllocateRegister(Variable variable, Register register) => true;
    }
}
