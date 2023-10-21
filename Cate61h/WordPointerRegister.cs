namespace Inu.Cate.Hd61700
{
    internal class WordPointerRegister : Cate.WordPointerRegister
    {
        public static List<Cate.PointerRegister> Registers = new();

        static WordPointerRegister()
        {
            foreach (var wordRegister in Hd61700.WordRegister.Registers) {
                Registers.Add(new WordPointerRegister(wordRegister));
            }
        }

        private WordPointerRegister(Cate.WordRegister wordRegister) : base(2, wordRegister) { }

        public override bool IsOffsetInRange(int offset)
        {
            return Compiler.IsOffsetInRange(offset);
        }

        public override void Add(Instruction instruction, int offset)
        {
            using var reservation = WordOperation.ReserveAnyRegister(instruction);
            var wordRegister = reservation.WordRegister;
            if (offset > 0) {
                wordRegister.LoadConstant(instruction, offset);
                instruction.WriteLine("\tadw " + AsmName + "," + wordRegister.AsmName);
            }
            else {
                wordRegister.LoadConstant(instruction, -offset);
                instruction.WriteLine("\tsbw " + AsmName + "," + wordRegister.AsmName);
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            WordRegister.Operate(instruction, operation, change, operand);
        }

        public static Register? FromIndex(int index)
        {
            return index < Registers.Count ? Registers[index] : null;
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            WordRegister.LoadConstant(instruction, value);
        }
    }
}
