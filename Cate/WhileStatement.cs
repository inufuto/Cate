using System.Diagnostics;

namespace Inu.Cate
{
    internal class WhileStatement : LoopStatement
    {
        private readonly BooleanValue condition;
        public Statement? Statement;

        public WhileStatement(BooleanValue condition, Function function) : base(function)
        {
            this.condition = condition;
        }

        public override void BuildInstructions(Function function)
        {
            ContinueAnchor.Address = function.NextAddress;
            condition.BuildJump(function, null, BreakAnchor);
            Debug.Assert(Statement != null);
            Statement.BuildInstructions(function);
            var jumpInstruction = Compiler.Instance.CreateJumpInstruction(function, ContinueAnchor);
            function.Instructions.Add(jumpInstruction);
            BreakAnchor.Address = function.NextAddress;
        }
    }
}