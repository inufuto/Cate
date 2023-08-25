namespace Inu.Cate.Mc6809
{
    internal class PointerLoadInstruction : Cate.PointerLoadInstruction
    {
        public PointerLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand)
        { }
    }
}
