namespace Inu.Cate.Tms99
{
    internal class PointerLoadInstruction : Cate.PointerLoadInstruction
    {
        public PointerLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (SourceOperand.SameStorage(DestinationOperand)) return;

            var source = Tms99.Compiler.OperandToString(this, SourceOperand, false);
            var destination = Tms99.Compiler.OperandToString(this, DestinationOperand, true);
            if (SourceOperand is NullPointerOperand && destination != null) {
                WriteLine("\tclr\t" + destination);
                return;
            }
            if (source != null & destination != null) {
                WriteLine("\tmov\t" + source + "," + destination);
                if (DestinationOperand.Register is WordRegister wordRegister) {
                    AddChanged(wordRegister);
                }
                if (DestinationOperand.Register is PointerRegister pointerRegister) {
                    AddChanged(pointerRegister);
                }
                return;
            }
            base.BuildAssembly();
        }
    }
}
