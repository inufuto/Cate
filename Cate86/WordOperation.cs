using System.Collections.Generic;

namespace Inu.Cate.I8086
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;
    }
}
