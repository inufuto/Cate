namespace Inu.Cate.MuCom87
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType) { }

        protected override void ExpandSigned()
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, SourceOperand);
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                WriteLine("\tshal");
                var candidates = ByteOperation.RegistersOtherThan(ByteRegister.A);
                using (var reservation = ByteOperation.ReserveAnyRegister(this, candidates)) {
                    var temporary = reservation.ByteRegister;
                    temporary.CopyFrom(this, ByteRegister.A);
                    WriteLine("\tsbb\ta," + temporary.AsmName);
                }
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            }
        }
    }
}
