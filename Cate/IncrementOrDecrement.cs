namespace Inu.Cate
{
    internal abstract class IncrementOrDecrement : Value
    {
        protected readonly int OperatorId;
        protected readonly AssignableValue SourceValue;

        protected IncrementOrDecrement(int operatorId, AssignableValue sourceValue) : base(sourceValue.Type)
        {
            OperatorId = operatorId;
            SourceValue = sourceValue;
        }

        public override void BuildInstructions(Function function)
        {
            var assignableOperand = SourceValue.ToAssignableOperand(function);
            var integerOperand = new IntegerOperand(SourceValue.Type, Type.Incremental);
            var binomialInstruction = Compiler.Instance.CreateBinomialInstruction(function, OperatorId == Keyword.Increment ? '+' : '-', assignableOperand, SourceValue.ToOperand(function), integerOperand);
            function.Instructions.Add(binomialInstruction);
        }
    }


    class PreIncrementOrDecrement : IncrementOrDecrement
    {
        public PreIncrementOrDecrement(int operatorId, AssignableValue sourceValue) : base(operatorId, sourceValue) { }

        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            var assignableOperand = SourceValue.ToAssignableOperand(function);
            var integerOperand = new IntegerOperand(SourceValue.Type, Type.Incremental);
            var binomialInstruction = Compiler.Instance.CreateBinomialInstruction(function, OperatorId == Keyword.Increment ? '+' : '-', assignableOperand, SourceValue.ToOperand(function), integerOperand);
            function.Instructions.Add(binomialInstruction);
            var loadInstruction = Compiler.Instance.CreateLoadInstruction(function, destinationOperand, assignableOperand);
            function.Instructions.Add(loadInstruction);
        }
    }



    class PostIncrementOrDecrement : IncrementOrDecrement
    {
        public PostIncrementOrDecrement(int operatorId, AssignableValue sourceValue) : base(operatorId, sourceValue) { }

        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            var assignableOperand = SourceValue.ToAssignableOperand(function);
            function.Instructions.Add(
                Compiler.Instance.CreateLoadInstruction(function, destinationOperand, assignableOperand));
            function.Instructions.Add(
                Compiler.Instance.CreateBinomialInstruction(function, OperatorId == Keyword.Increment ? '+' : '-', assignableOperand, assignableOperand, new IntegerOperand(SourceValue.Type, Type.Incremental)));
        }
    }
}