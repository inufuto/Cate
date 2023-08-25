using System.Collections.Generic;

namespace Inu.Cate.Mc6809
{
    internal class PointerOperation : Cate.PointerOperation
    {
        public override List<Cate.PointerRegister> Registers => PointerRegister.IndexRegisters;
    }
}
