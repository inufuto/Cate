using System.Diagnostics;

namespace Inu.Cate
{
    class DefaultStatement : Statement
    {
        public readonly Anchor Anchor;
        public Statement? Statement;

        public DefaultStatement(Function function)
        {
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