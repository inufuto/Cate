namespace Inu.Cate.Sc62015
{
    internal class PointerInternalRam : PointerRegister
    {
        public const int Count = 8;
        public static readonly PointerInternalRam SI = new("si", "0dah");
        public static readonly PointerInternalRam DI = new("di", "0ddh");

        public new static readonly List<Cate.PointerRegister> Registers = new();

        static PointerInternalRam()
        {
            Registers.Add(SI);
            Registers.Add(DI);
            for (var index = 0; index < Count; index++) {
                Registers.Add(new PointerInternalRam(index));
            }
        }
        
        private static string IndexToName(int index)
        {
            return "(" + IndexToLabel(index) + ")";
        }

        private static string IndexToLabel(int index)
        {
            return ("<@p" + index);
        }

        public override string MV => "mvp";
        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tmvp [--s]," + Name + "\t" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tmvp " + Name + ",[s++]\t" + comment);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tmvp [--s]," + Name + "\t");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tmvp " + Name + ",[s++]\t");
        }
        public readonly string Label;


        public PointerInternalRam(int index) : this(IndexToName(index), IndexToLabel(index)) { }

        private PointerInternalRam(string name, string label) : base(name)
        {
            Label = label;
        }
    }
}
