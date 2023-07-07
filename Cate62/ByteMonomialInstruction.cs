namespace Inu.Cate.Sc62015
{
    internal class ByteMonomialInstruction : MonomialInstruction
    {
        public ByteMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            Action<Cate.ByteRegister> operation = OperatorId switch
            {
                '-' => r =>
                {
                    WriteLine("\txor " + r.AsmName + ",0ffh");
                    WriteLine("\tinc " + r.AsmName);
                }
                ,
                '~' => r =>
                {
                    WriteLine("\txor " + r.AsmName + ",0ffh");
                }
                ,
                _ => throw new NotImplementedException()
            };

            void ViaRegister(Cate.ByteRegister r)
            {
                r.Load(this, SourceOperand);
                operation(r);
                r.Store(this, DestinationOperand);
            }

            if (Equals(DestinationOperand.Register, ByteRegister.A) || DestinationOperand.Register is ByteInternalRam) {
                ViaRegister((Cate.ByteRegister)DestinationOperand.Register);
                return;
            }

            var candidates = ByteRegister.AccumulatorAndInternalRam;
            using var reservation = ByteOperation.ReserveAnyRegister(this, candidates, SourceOperand);
            ViaRegister(reservation.ByteRegister);
        }
    }
}
