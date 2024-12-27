using System.Collections.Generic;

namespace Inu.Cate.Mc6800.Mc6801;

internal class WordOperation : Mc6800.WordOperation
{
    public override List<WordRegister> Registers => [PairRegister.D, IndexRegister.X];
}