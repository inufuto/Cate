using System.Collections.Generic;

namespace Inu.Cate.Mc6800.Mc6801;

internal class PointerOperation : Cate.PointerOperation
{
    public override List<Cate.PointerRegister> Registers => PointerRegister.Registers;
}