namespace Inu.Cate
{
    internal class ReturnStatement : JumpStatement
    {
        private readonly Value? value;
        public override Anchor Anchor { get; }

        public ReturnStatement(Value? value, Anchor anchor)
        {
            this.value = value;
            Anchor = anchor;
        }

        public override void BuildInstructions(Function function)
        {
            if (value != null) {
                var operand = value.ToOperand(function);
                var instruction = Compiler.Instance.CreateReturnInstruction(function, operand, function.ExitAnchor);
                function.Instructions.Add(instruction);
            }
            else {
                var instruction = Compiler.Instance.CreateJumpInstruction(function, function.ExitAnchor);
                function.Instructions.Add(instruction);
            }
        }

        public override bool HasOperand() => value != null;
    }
}