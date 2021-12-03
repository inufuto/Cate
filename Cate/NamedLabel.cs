using Inu.Language;

namespace Inu.Cate
{
    public class NamedLabel
    {
        public readonly Token Identifier;
        public Anchor? Anchor;

        public NamedLabel(Token identifier)
        {
            Identifier = identifier;
        }
    }
}