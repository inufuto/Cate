namespace Inu.Cate.Sc62015
{
    internal class ByteInternalRam : ByteRegister
    {
        public const int Count = 8;
        public static readonly ByteInternalRam BL = new ByteInternalRam("bl");
        public static readonly ByteInternalRam BH = new ByteInternalRam("bh");
        public static readonly ByteInternalRam CL = new ByteInternalRam("cl");
        public static readonly ByteInternalRam CH = new ByteInternalRam("ch");
        public static readonly ByteInternalRam DL = new ByteInternalRam("dl");
        public static readonly ByteInternalRam DH = new ByteInternalRam("dh");

        public new static List<Cate.ByteRegister> Registers
        {
            get
            {
                var registers = new List<Cate.ByteRegister>() { BL, BH, CL, CH, DL, DH };
                for (var index = 0; index < Count; index++) {
                    registers.Add(new ByteInternalRam(index));
                }
                return registers;
            }
        }

        private static string IndexToName(int id)
        {
            return "(" + Label(id) + ")";
        }

        private static string Label(int index)
        {
            return ("<@b" + index);
        }

        public override Cate.WordRegister? PairRegister => WordInternalRam.Registers.FirstOrDefault(w => w.Contains(this));

        public ByteInternalRam(int index) : base(IndexToName(index),null) { }

        private ByteInternalRam(string name) : base(name, null) { }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tmv [--s]," + Name + "\t" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tmv " + Name + ",[s++]\t" + comment);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tmv [--s]," + Name + "\t");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tmv " + Name + ",[s++]\t");
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (operand is ConstantOperand constantOperand) {
                Operate(instruction, operation, change, constantOperand.MemoryAddress());
            }
            else {
                using var reservation = ByteOperation.ReserveAnyRegister(instruction,
                    ByteOperation.Registers, operand);
                var operandRegister = reservation.ByteRegister;
                operandRegister.Load(instruction, operand);
                Operate(instruction, operation, change, operandRegister.Name);
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            instruction.WriteLine("\t" + operation + " " + Name + "," + operand);
            if (change) {
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
        }
    }
}
