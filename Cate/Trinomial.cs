namespace Inu.Cate
{
    internal class Trinomial : Value
    {
        private readonly BooleanValue conditionValue;
        private readonly Value trueValue;
        private readonly Value falseValue;

        public Trinomial(ParameterizableType type, BooleanValue conditionValue, Value trueValue, Value falseValue) : base(type)
        {
            this.conditionValue = conditionValue;
            this.trueValue = trueValue;
            this.falseValue = falseValue;
        }

        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            var falseAnchor = function.CreateAnchor();
            var endAnchor = function.CreateAnchor();
            conditionValue.BuildJump(function, null, falseAnchor);
            trueValue.BuildInstructions(function, destinationOperand);
            function.Instructions.Add(Compiler.Instance.CreateJumpInstruction(function, endAnchor));
            falseAnchor.Address = function.NextAddress;
            falseValue.BuildInstructions(function, destinationOperand);
            endAnchor.Address = function.NextAddress;
        }

        public override void BuildInstructions(Function function)
        {
            var falseAnchor = function.CreateAnchor();
            var endAnchor = function.CreateAnchor();
            conditionValue.BuildJump(function, null, falseAnchor);
            trueValue.BuildInstructions(function);
            function.Instructions.Add(Compiler.Instance.CreateJumpInstruction(function, endAnchor));
            falseAnchor.Address = function.NextAddress;
            falseValue.BuildInstructions(function);
            endAnchor.Address = function.NextAddress;
        }
    }
}
