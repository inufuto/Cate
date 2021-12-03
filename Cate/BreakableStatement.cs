namespace Inu.Cate
{
    abstract class BreakableStatement : Statement
    {
        public readonly Anchor BreakAnchor;

        protected BreakableStatement(Function function)
        {
            BreakAnchor = function.CreateAnchor();
        }
    }
}