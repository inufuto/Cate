namespace Inu.Cate
{
    class BreakOrContinueStatement : JumpStatement
    {
        public override Anchor Anchor { get; }


        public BreakOrContinueStatement(Anchor anchor)
        {
            Anchor = anchor;
        }
    }
}