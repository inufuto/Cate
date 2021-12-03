using System.Diagnostics;

namespace Inu.Cate
{
    internal class ForStatement : LoopStatement
    {
        private readonly Value? initialize;
        private readonly BooleanValue condition;
        private readonly Value? update;
        public Statement? Statement;

        public ForStatement(Value? initialize, BooleanValue condition, Value? update, Function function) : base(function)
        {
            this.initialize = initialize;
            this.condition = condition;
            this.update = update;
        }

        public override void BuildInstructions(Function function)
        {
            initialize?.BuildInstructions(function);

            ContinueAnchor.Address = function.NextAddress;
            condition.BuildJump(function, null, BreakAnchor);
            Debug.Assert(Statement != null);
            Statement.BuildInstructions(function);

            update?.BuildInstructions(function);

            var jumpInstruction = Compiler.Instance.CreateJumpInstruction(function, ContinueAnchor);
            function.Instructions.Add(jumpInstruction);
            BreakAnchor.Address = function.NextAddress;
        }
    }
}