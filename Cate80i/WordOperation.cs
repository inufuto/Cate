using System.Collections.Generic;

namespace Inu.Cate.I8080
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;

        public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset, int value)
        {
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    instruction.WriteLine("\tmvi\tm,low(" + value + ")");
                    instruction.WriteLine("\tinx\t" + pointerRegister.AsmName);
                    instruction.WriteLine("\tmvi\tm,high(" + value + ")");
                    instruction.WriteLine("\tdcx\t" + pointerRegister.AsmName);
                    return;
                }
            }
            if (Equals(pointerRegister, PointerRegister.Hl)) {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    StoreConstantIndirect(instruction, pointerRegister, 0, value);
                });
                return;
            }
            base.StoreConstantIndirect(instruction, pointerRegister, offset, value);
        }
    }
}
