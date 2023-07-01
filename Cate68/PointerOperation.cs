using System.Collections.Generic;

namespace Inu.Cate.Mc6800
{
    internal class PointerOperation : Cate.PointerOperation
    {
        public override List<Cate.PointerRegister> Registers => PointerRegister.Registers;
    }
}
