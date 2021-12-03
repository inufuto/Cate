using System.Diagnostics;

namespace Inu.Cate
{
    class DoStatement : LoopStatement
    {
        private readonly Anchor repeatAnchor;
        public Statement? Statement;
        public BooleanValue? Condition;


        public DoStatement(Function function) : base(function)
        {
            repeatAnchor = function.CreateAnchor();
        }

        public override void BuildInstructions(Function function)
        {
            repeatAnchor.Address = function.NextAddress;
            Debug.Assert(Statement != null);
            Statement.BuildInstructions(function);
            ContinueAnchor.Address = function.NextAddress;
            Debug.Assert(Condition != null);
            Condition.BuildJump(function, repeatAnchor, null);
            BreakAnchor.Address = function.NextAddress;
        }
    }
}