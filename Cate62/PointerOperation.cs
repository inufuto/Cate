using Microsoft.Win32;

namespace Inu.Cate.Sc62015
{
    internal class PointerOperation : Cate.PointerOperation
    {
        private readonly List<Cate.PointerRegister> registers = new();
        
        public override List<Cate.PointerRegister> Registers => registers;

        public PointerOperation()
        {
            registers.AddRange(PointerRegister.Registers);
            registers.AddRange(PointerInternalRam.Registers);
        }
    }
}
