using System.Collections.Generic;

namespace Inu.Cate.Z80
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;
    }
}
