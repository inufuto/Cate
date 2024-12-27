namespace Inu.Cate.Sc62015
{
    internal class WordOperation : Cate.WordOperation
    {
        private readonly List<Cate.WordRegister> registers = new();
        public override List<Cate.WordRegister> Registers => registers;

        public WordOperation()
        {
            registers.AddRange(WordRegister.Registers);
            registers.AddRange(WordInternalRam.Registers);
            registers.AddRange(PointerRegister.Registers);
            registers.AddRange(PointerInternalRam.Registers);
        }

        public override List<Cate.WordRegister> RegistersToOffset(int offset)
        {
            return PointerRegister.Registers.Where(r => ((PointerRegister)r).IsOffsetInRange(offset)).ToList();
        }
    }
}
