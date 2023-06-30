using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;
        public override List<Cate.WordRegister> PointerRegisters(int offset)
        {
            return WordRegister.Pointers(offset);
        }
    }
}
