﻿namespace Inu.Cate.Sc62015
{
    internal class PointerInternalRam : PointerRegister
    {
        public const string Prefix = "@wp";
        public const int Count = 8;
        public static readonly PointerInternalRam SI = new("si", "0dah");
        public static readonly PointerInternalRam DI = new("di", "0ddh");

        public new static readonly List<Cate.WordRegister> Registers = [];

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
            return ("<" + Prefix + "+" + index + "*3");
        }

        public override string MV => "mvp";
        public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tmvp [--s]," + Name + "\t" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
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

        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            switch (sourceRegister) {
                case WordInternalRam:
                    instruction.WriteLine("\tmvw " + AsmName + "," + sourceRegister.AsmName);
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
                case WordRegister wordRegister: {
                        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordInternalRam.Registers);
                        reservation.WordRegister.CopyFrom(instruction, wordRegister);
                        CopyFrom(instruction, reservation.WordRegister);
                        return;
                    }
            }
            base.CopyFrom(instruction, sourceRegister);
        }

        public readonly string Label;


        public PointerInternalRam(int index) : this(IndexToName(index), IndexToLabel(index)) { }

        private PointerInternalRam(string name, string label) : base(name)
        {
            Label = label;
        }
    }
}
