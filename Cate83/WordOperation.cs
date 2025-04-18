﻿namespace Inu.Cate.Sm83;

internal class WordOperation:Cate.WordOperation
{
    public override List<Cate.WordRegister> Registers => WordRegister.Registers;

    public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset, int value)
    {
        if (offset == 0) {
            if (Equals(pointerRegister, WordRegister.Hl)) {
                instruction.WriteLine("\tld\t(" + pointerRegister + "),low(" + value + ")");
                instruction.WriteLine("\tinc\t" + pointerRegister.AsmName);
                instruction.WriteLine("\tld\t(" + pointerRegister + "),high(" + value + ")");
                instruction.WriteLine("\tdec\t" + pointerRegister.AsmName);
                return;
            }
        }
        base.StoreConstantIndirect(instruction, pointerRegister, offset, value);
    }
}