using System;
using System.Collections.Generic;

namespace Inu.Cate.Mos6502
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordZeroPage.Registers;
    }
}