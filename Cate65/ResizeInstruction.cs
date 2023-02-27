namespace Inu.Cate.Mos6502
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType)
        { }

        protected override void ExpandSigned()
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, SourceOperand);
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                ByteRegister.A.Operate(this, "asl", true, 1);
                using (var reservation = ByteOperation.ReserveAnyRegister(this, ByteZeroPage.Registers)) {
                    var temporary = reservation.ByteRegister;
                    temporary.CopyFrom(this, ByteRegister.A);
                    ByteRegister.A.Operate(this, "sbc", true, temporary.Name);
                }
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            }
        }
    }
}