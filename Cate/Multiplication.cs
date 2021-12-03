using System.Diagnostics;

namespace Inu.Cate
{
    internal class Multiplication : Value
    {
        private readonly Value leftValue;
        private readonly int rightValue;

        public Multiplication(Value leftValue, int rightValue) : base(leftValue.Type)
        {
            Debug.Assert(leftValue.Type is IntegerType);
            this.leftValue = leftValue;
            this.rightValue = rightValue;
        }

        public override void BuildInstructions(Function function, AssignableOperand destinationOperand)
        {
            var leftOperand = leftValue.ToOperand(function);
            var instruction = Compiler.Instance.CreateMultiplyInstruction(
                function, 
                destinationOperand, 
                leftOperand, 
                rightValue);
            function.Instructions.Add(instruction);
        }

        public override void BuildInstructions(Function function)
        {
            leftValue.BuildInstructions(function);
        }
    }
}