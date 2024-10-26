using System.Collections.Generic;

namespace Inu.Cate.Z80;

internal class WordOperation : Cate.WordOperation
{
    public override List<Cate.WordRegister> Registers => WordRegister.Registers;

    public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset, int value)
    {
        if (pointerRegister is IndexRegister && pointerRegister.IsOffsetInRange(offset)) {
            instruction.WriteLine("\tld\t(" + pointerRegister + "+" + offset + "),low(" + value+")");
            instruction.WriteLine("\tld\t(" + pointerRegister + "+" + offset + "+1),high(" + value + ")");
            return;
        }
        if (offset == 0) {
            if (PointerRegister.IsAddable(pointerRegister)) {
                instruction.WriteLine("\tld\t(" + pointerRegister + "),low(" + value+")");
                instruction.WriteLine("\tinc\t" + pointerRegister.AsmName);
                instruction.WriteLine("\tld\t(" + pointerRegister + "),high(" + value + ")");
                instruction.WriteLine("\tdec\t" + pointerRegister.AsmName);
                return;
            }
        }
        base.StoreConstantIndirect(instruction, pointerRegister, offset, value);
    }
}