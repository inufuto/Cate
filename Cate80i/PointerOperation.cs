using System.Collections.Generic;

namespace Inu.Cate.I8080
{
    internal class PointerOperation : Cate.PointerOperation
    {
        public override List<Cate.PointerRegister> Registers => PointerRegister.Registers;
    }
}
