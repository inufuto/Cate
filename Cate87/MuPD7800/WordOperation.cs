using System.Diagnostics;

namespace Inu.Cate.MuCom87.MuPD7800;

internal class WordOperation: MuCom87.WordOperation
{
    public override int Threshold => 4;
    public override void AddRegister(Instruction instruction, WordRegister wordRegister, int offset)
    {
        Debug.Assert(wordRegister.Low != null);
        Debug.Assert(wordRegister.High != null);
        instruction.WriteLine("\tadi\t" + wordRegister.Low.AsmName + ",low(" + offset + ")");
        instruction.WriteLine("\taci\t" + wordRegister.High.AsmName + ",high(" + offset + ")");
    }

    public override void SubtractRegister(Instruction instruction, WordRegister wordRegister, int offset)
    {
        Debug.Assert(wordRegister.Low != null);
        Debug.Assert(wordRegister.High != null);
        instruction.WriteLine("\tsui\t" + wordRegister.Low.AsmName + ",low(" + offset + ")");
        instruction.WriteLine("\tsbi\t" + wordRegister.High.AsmName + ",high(" + offset + ")");
    }
}