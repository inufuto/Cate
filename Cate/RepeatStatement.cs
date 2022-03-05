using System.Diagnostics;

namespace Inu.Cate
{
    internal class RepeatStatement : LoopStatement
    {
        private readonly int count;
        public Statement? Statement;

        public RepeatStatement(int count, Function function) : base(function)
        {
            this.count = count;
        }

        public override void BuildInstructions(Function function)
        {
            var compiler = Compiler.Instance;
            var counterType = Compiler.Instance.CounterType;
            var counter = function.CreateTemporaryVariable(counterType);

            var loadInstruction = compiler.CreateLoadInstruction(function, counter.ToAssignableOperand(), new IntegerOperand(counterType, count));
            function.Instructions.Add(loadInstruction);
            var repeatAnchor = function.CreateAnchor();
            repeatAnchor.Address = function.NextAddress;

            Debug.Assert(Statement != null);
            Statement.BuildInstructions(function);

            ContinueAnchor.Address = function.NextAddress;
            var jumpInstruction = compiler.CreateDecrementJumpInstruction(function, counter.ToAssignableOperand(), repeatAnchor);
            function.Instructions.Add(jumpInstruction);

            BreakAnchor.Address = function.NextAddress;
        }
    }
}