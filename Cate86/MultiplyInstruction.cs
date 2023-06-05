namespace Inu.Cate.I8086
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                if (DestinationOperand.Register is WordRegister wordRegister) {
                    wordRegister.LoadConstant(this, 0);
                    return;
                }

                using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers);
                var register = reservation.WordRegister;
                register.LoadConstant(this, 0);
                register.Store(this, DestinationOperand);
                return;
            }
            if (LeftOperand.Type.ByteCount == 1) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.Ah)) {
                    ByteRegister.Al.Load(this, LeftOperand);
                    WriteLine("\tmov ah," + RightValue);
                    WriteLine("\tmul ah");
                    AddChanged(ByteRegister.Ah);
                    if (DestinationOperand.Type.ByteCount == 1) {
                        ByteRegister.Al.Store(this, DestinationOperand);
                        AddChanged(ByteRegister.Al);
                    }
                    else {
                        WordRegister.Ax.Store(this, DestinationOperand);
                        AddChanged(WordRegister.Ax);
                    }
                }
                return;
            }
            using (WordOperation.ReserveRegister(this, WordRegister.Dx)) {
                WordRegister.Ax.Load(this, LeftOperand);
                WriteLine("\tmov dx," + RightValue);
                WriteLine("\tmul dx");
                AddChanged(WordRegister.Dx);
                if (DestinationOperand.Type.ByteCount == 1) {
                    ByteRegister.Al.Store(this, DestinationOperand);
                    AddChanged(ByteRegister.Al);
                }
                else {
                    WordRegister.Ax.Store(this, DestinationOperand);
                    AddChanged(WordRegister.Ax);
                }
            }
        }
    }
}
