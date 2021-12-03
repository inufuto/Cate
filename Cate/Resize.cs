namespace Inu.Cate
{
    internal class Resize : Value
    {
        private readonly Value sourceValue;
        private readonly IntegerType sourceType;
        private readonly IntegerType destinationType;

        public Resize(Value sourceValue, IntegerType sourceType, IntegerType destinationType) : base(destinationType)
        {
            this.sourceValue = sourceValue;
            this.sourceType = sourceType;
            this.destinationType = destinationType;
        }

        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            Operand sourceOperand = sourceValue.ToOperand(function);
            var instruction = Compiler.Instance.CreateResizeInstruction(
                function, 
                destinationOperand, 
                destinationType, 
                sourceOperand, 
                sourceType);
            function.Instructions.Add(instruction);
        }

        public override void BuildInstructions(Function function)
        {
            sourceValue.BuildInstructions(function);
        }
    }
}