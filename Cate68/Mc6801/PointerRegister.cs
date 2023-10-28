using System;
using System.Collections.Generic;

namespace Inu.Cate.Mc6800.Mc6801;

internal class PointerRegister : Mc6800.PointerRegister
{
    public new static PointerRegister X = new(Mc6801.IndexRegister.X);

    public new static List<Cate.PointerRegister> Registers => new() { X };
    public override void Add(Instruction instruction, int offset)
    {
        if (Math.Abs(offset) > 10) {
            using (WordOperation.ReserveRegister(instruction, PairRegister.D)) {
                PairRegister.D.CopyFrom(instruction, WordRegister);
                if (offset > 0) {
                    instruction.WriteLine("\taddd\t#" + offset);
                }
                else {
                    instruction.WriteLine("\tsubd\t#" + -offset);
                }
                WordRegister.CopyFrom(instruction, PairRegister.D);
                return;
            }
        }
        base.Add(instruction, offset);
    }

    private PointerRegister(WordRegister wordRegister) : base(wordRegister) { }
}