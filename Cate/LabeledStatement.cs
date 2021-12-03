using System.Diagnostics;

namespace Inu.Cate
{
    class LabeledStatement : Statement
    {
        private readonly NamedLabel namedLabel;
        private readonly Statement statement;

        public LabeledStatement(NamedLabel namedLabel, Statement statement)
        {
            this.namedLabel = namedLabel;
            this.statement = statement;
        }

        public override void BuildInstructions(Function function)
        {
            Debug.Assert(namedLabel.Anchor != null);
            namedLabel.Anchor.Address = function.NextAddress;
            statement.BuildInstructions(function);
        }
    }
}