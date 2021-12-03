namespace Inu.Cate
{
    abstract class LoopStatement : BreakableStatement
    {
        public readonly Anchor ContinueAnchor;

        protected LoopStatement(Function function) : base(function)
        {
            ContinueAnchor = function.CreateAnchor();
        }
    }
}