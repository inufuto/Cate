namespace Inu.Cate
{
    internal class TypeChange : Value
    {
        private readonly Value sourceValue;

        public TypeChange(ParameterizableType type, Value sourceValue) : base(type)
        {
            this.sourceValue = sourceValue;
        }

        public new ParameterizableType Type => (ParameterizableType)base.Type;
        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            var sourceOperand = sourceValue.ToOperand(function);
            var instruction = Compiler.Instance.CreateLoadInstruction(
                function, 
                destinationOperand,
                sourceOperand);
            function.Instructions.Add(instruction);
        }

        public override void BuildInstructions(Function function)
        {
            sourceValue.BuildInstructions(function);
        }

        public override Operand ToOperand(Function function)
        {
            return sourceValue.ToOperand(function).Cast(Type);
        }
    }
}