using System.Linq;

namespace Inu.Cate
{
    internal class Binomial : Value
    {
        private readonly int operatorId;
        private Value leftValue;
        private Value rightValue;

        public Binomial(ParameterizableType type, int operatorId, Value leftValue, Value rightValue) : base(type)
        {
            this.operatorId = operatorId;
            this.leftValue = leftValue;
            this.rightValue = rightValue;
        }

        public new ParameterizableType Type => (ParameterizableType)base.Type;

        private static readonly int[] ExchangeableOperators = { '|', '^', '&', '+' };

        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            {
                if (leftValue.IsConstant() && !rightValue.IsConstant() && ExchangeableOperators.Contains(operatorId)) {
                    var temporary = leftValue;
                    leftValue = rightValue;
                    rightValue = temporary;
                }

                //Debug.Assert(leftOperand != null && rightOperand != null);
                var leftOperand = leftValue.ToOperand(function);
                var rightOperand = rightValue.ToOperand(function);
                var instruction = Compiler.Instance.CreateBinomialInstruction(
                    function, operatorId,
                    destinationOperand,
                    leftOperand,
                    rightOperand);
                function.Instructions.Add(instruction);
            }
        }

        public override void BuildInstructions(Function function)
        {
            leftValue.BuildInstructions(function);
            rightValue.BuildInstructions(function);
        }
    }
}