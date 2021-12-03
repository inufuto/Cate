namespace Inu.Cate
{
    internal class Assignment : Value
    {
        private readonly AssignableValue leftValue;
        private readonly Value rightValue;

        public Assignment(AssignableValue leftValue, Value rightValue) : base(leftValue.Type)
        {
            this.leftValue = leftValue;
            this.rightValue = rightValue;
        }

        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            var leftOperand = leftValue.ToAssignableOperand(function);
            var rightOperand = rightValue.ToOperand(function);
            function.Instructions.Add(Compiler.CreateLoadInstruction(function, leftOperand, rightOperand));
            function.Instructions.Add(Compiler.CreateLoadInstruction(function, destinationOperand, leftOperand));
        }

        public override void BuildInstructions(Function function)
        {
            var destinationOperand = leftValue.ToAssignableOperand(function);
            rightValue.BuildInstructions(function, destinationOperand);
        }

        public override Operand ToOperand(Function function)
        {
            switch (leftValue) {
                case VariableValue variableValue: {
                        rightValue.BuildInstructions(function, variableValue.ToAssignableOperand(function));
                        return variableValue.ToOperand(function);
                    }
                default: {
                        var variable = function.CreateTemporaryVariable(Type);
                        rightValue.BuildInstructions(function, variable.ToAssignableOperand());
                        var leftOperand = leftValue.ToAssignableOperand(function);
                        var temporaryOperand = variable.ToOperand();
                        function.Instructions.Add(Compiler.CreateLoadInstruction(function, leftOperand, temporaryOperand));
                        return temporaryOperand;
                    }
            }
        }
    }
}