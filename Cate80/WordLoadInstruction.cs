namespace Inu.Cate.Z80
{
    internal class WordLoadInstruction : Cate.WordLoadInstruction
    {
        public WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
            : base(function, destinationOperand, sourceOperand)
        { }
    }
}