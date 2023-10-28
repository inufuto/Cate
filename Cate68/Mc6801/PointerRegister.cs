using System.Collections.Generic;

namespace Inu.Cate.Mc6800.Mc6801;

internal class PointerRegister : Mc6800.PointerRegister
{
    public new static PointerRegister X = new(Mc6801.IndexRegister.X);
    public new static PointerRegister D = new(Mc6801.PairRegister.D);

    public new static List<Cate.PointerRegister> Registers => new() { X, D };

    private PointerRegister(WordRegister wordRegister) : base(wordRegister) { }
}