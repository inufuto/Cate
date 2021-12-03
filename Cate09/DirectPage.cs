namespace Inu.Cate.Mc6809
{
    internal abstract class DirectPage
    {
        public readonly string Label;

        public static readonly DirectPageByte Byte = new DirectPageByte("@Temp@Byte");
        public static readonly DirectPageWord Word = new DirectPageWord("@Temp@Word");
        public static readonly DirectPageWord Word2 = new DirectPageWord("@Temp@Word2");
        public static readonly DirectPageWord Word3 = new DirectPageWord("@Temp@Word3");

        protected DirectPage(string name)
        {
            Label = name;
        }

        public override string ToString()
        {
            return "<" + Label;
        }
    }

    internal class DirectPageByte : DirectPage
    {
        public DirectPageByte(string name) : base(name)
        { }
    }

    internal class DirectPageWord : DirectPage
    {
        public DirectPageWord(string name) : base(name)
        { }

        public DirectPageByte Low => new DirectPageByte(Label + "+1");
        public DirectPageByte High => new DirectPageByte(Label + "+0");
    }
}