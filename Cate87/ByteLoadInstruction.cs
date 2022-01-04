namespace Inu.Cate.MuCom87
{
    internal class ByteLoadInstruction : Cate.ByteLoadInstruction
    {
        public ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (
                DestinationOperand.SameStorage(SourceOperand) &&
                DestinationOperand.Type.ByteCount == SourceOperand.Type.ByteCount
            ) return;
            if (DestinationOperand.Register is ByteRegister destinationRegister && SourceOperand is ConstantOperand) {
                destinationRegister.Load(this, SourceOperand);
                return;
            }
            if (Equals(SourceOperand.Register, ByteRegister.A)) {
                ByteRegister.A.Store(this, DestinationOperand);
                return;
            }
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, SourceOperand);
                ByteRegister.A.Store(this, DestinationOperand);
            });
            //base.BuildAssembly();
        }
    }
}
