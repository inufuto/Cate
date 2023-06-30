namespace Inu.Cate.Sc62015
{
    internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            throw new NotImplementedException();
        }

        protected override int Threshold() => 2;

        protected override void Increment(int count)
        {
            OperateByte("inc\t", count);
            ResultFlags |= Flag.Z;
        }

        protected override void Decrement(int count)
        {
            OperateByte("dec\t", count);
            ResultFlags |= Flag.Z;
        }
    }
}
