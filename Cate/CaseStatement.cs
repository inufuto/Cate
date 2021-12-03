using System.Diagnostics;

namespace Inu.Cate
{
    class CaseStatement : Statement
    {
        public readonly ConstantInteger Value;
        public readonly Anchor Anchor;
        public Statement? Statement;

        public CaseStatement(ConstantInteger value, Function function)
        {
            Value = value;
            Anchor = function.CreateAnchor();
        }

        public override void BuildInstructions(Function function)
        {
            Anchor.Address = function.NextAddress;
            Debug.Assert(Statement != null);
            Statement.BuildInstructions(function);
        }
    }
}