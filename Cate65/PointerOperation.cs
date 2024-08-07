using System.Collections.Generic;

namespace Inu.Cate.Mos6502;

internal class PointerOperation : Cate.PointerOperation
{
    public override List<PointerRegister> Registers => PointerZeroPage.Registers;
}