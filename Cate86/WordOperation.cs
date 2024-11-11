using System.Collections.Generic;

namespace Inu.Cate.I8086;

internal class WordOperation : Cate.WordOperation
{
    public override List<Cate.WordRegister> Registers => WordRegister.Registers;
    public override List<Cate.WordRegister> RegistersForType(Type type)
    {
        return type is PointerType ? WordRegister.PointerRegisters : WordRegister.Registers;
    }
}