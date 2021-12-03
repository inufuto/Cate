using System.Collections.Generic;

namespace Inu.Cate
{
    class CompositeStatement : Statement
    {
        public readonly Block Block;
        public readonly List<Statement> Statements = new List<Statement>();

        public CompositeStatement(Block block)
        {
            Block = block;
        }

        public override void BuildInstructions(Function function)
        {
            foreach (var statement in Statements) {
                statement.BuildInstructions(function);
            }
            Block.End(function);
        }
    }
}