namespace Inu.Cate
{
    class GotoStatement : JumpStatement
    {
        private readonly NamedLabel namedLabel;

        public GotoStatement(NamedLabel namedLabel)
        {
            this.namedLabel = namedLabel;
        }

        public override Anchor Anchor {
            get {
                if (namedLabel.Anchor == null) {
                    throw new UndefinedIdentifierError(namedLabel.Identifier);
                }
                return namedLabel.Anchor;
            }
        }
    }
}