using System.Diagnostics;

namespace Inu.Cate
{
    class Monomial : Value
    {
        private readonly int operatorId;
        private readonly Value sourceValue;

        public Monomial(ParameterizableType type, int operatorId, Value operand) : base(type)
        {
            this.operatorId = operatorId;
            sourceValue = operand;
        }

        public new ParameterizableType Type => (ParameterizableType)base.Type;


        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            var sourceOperand = sourceValue.ToOperand(function);
            Debug.Assert(sourceOperand != null);
            var instruction = Compiler.Instance.CreateMonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
            function.Instructions.Add(instruction);
        }

        public override void BuildInstructions(Function function)
        {
            sourceValue.BuildInstructions(function);
        }
    }
}