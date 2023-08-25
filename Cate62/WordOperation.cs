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
        }
    }
}
