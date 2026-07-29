using System.Collections.Generic;

namespace Inu.Cate.MuCom87;

internal abstract class WordOperation : Cate.WordOperation
{
    public override List<Cate.WordRegister> Registers => WordRegister.Registers;

    public abstract int Threshold { get; }

    protected override bool CanCopyRegisterToSave(Instruction instruction, Cate.WordRegister register) => false;
    public abstract void AddRegister(Instruction instruction, WordRegister wordRegister, int offset);
    public abstract void SubtractRegister(Instruction instruction, WordRegister wordRegister, int offset);
}