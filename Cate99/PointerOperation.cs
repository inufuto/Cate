using System.Collections.Generic;

namespace Inu.Cate.Tms99
{
    internal class PointerOperation : Cate.PointerOperation
    {
        public override List<Cate.PointerRegister> Registers => PointerRegister.Registers;
    }
}
