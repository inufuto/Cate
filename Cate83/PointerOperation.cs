namespace Inu.Cate.Sm83;

internal class PointerOperation:Cate.PointerOperation
{
    public override List<Cate.PointerRegister> Registers => PointerRegister.Registers;

    public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset, int value)
    {
        if (offset == 0) {
            if (Equals(pointerRegister, PointerRegister.Hl)) {
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