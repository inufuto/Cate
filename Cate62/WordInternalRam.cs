namespace Inu.Cate.Sc62015
{
    internal class WordInternalRam : WordRegister
    {
        public const int Count = 8;
        public static readonly WordInternalRam BX = new("bx", "0d4h", ByteInternalRam.BL, ByteInternalRam.BH);
        public static readonly WordInternalRam CX = new("cx", "0d6h", ByteInternalRam.CL, ByteInternalRam.CH);
        public static readonly WordInternalRam DX = new("dx", "0d8h", ByteInternalRam.DL, ByteInternalRam.DH);

        public new static List<Cate.WordRegister> Registers = new();

        static WordInternalRam()
        {
            Registers.Add(BX);
            Registers.Add(CX);
            Registers.Add(DX);
            for (var index = 0; index < Count; index++) {
                Registers.Add(new WordInternalRam(index));
            }
        }

        private static string IndexToName(int index)
        {
            return "(" + IndexToLabel(index) + ")";
        }

        private static string IndexToLabel(int index)
        {
            return ("<@w" + index);
        }

        public override Cate.ByteRegister? Low { get; }
        public override Cate.ByteRegister? High { get; }
        public override string MV => "mvw";
        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tmvw [--s]," + Name + "\t" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tmvw " + Name + ",[s++]\t" + comment);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tmvw [--s]," + Name + "\t");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tmvw " + Name + ",[s++]\t");
        }

        public readonly string Label;

        public WordInternalRam(int index) : this(IndexToName(index), IndexToLabel(index), null,null) { }

        private WordInternalRam(string name, string label, Cate.ByteRegister? low, Cate.ByteRegister? high) : base(name, low, high)
        {
            Label = label;
            Low = low;
            High = high;
        }
    }
}
