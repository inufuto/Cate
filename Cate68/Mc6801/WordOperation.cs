using System.Collections.Generic;

namespace Inu.Cate.Mc6800.Mc6801;

internal class WordOperation : Mc6800.WordOperation
{
    public override List<WordRegister> Registers => [PairRegister.D, IndexRegister.X];
    public override List<WordRegister> RegistersForType(Type type)
    {
        return type is PointerType ? ([IndexRegister.X,]) : base.RegistersForType(type);
    }
}