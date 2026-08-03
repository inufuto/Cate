using System.Diagnostics;

namespace Inu.Cate.MuCom87.MuPd7805;

internal class WordOperation: MuCom87.WordOperation
{
    public override int Threshold => 8;
    public override void AddRegister(Instruction instruction, WordRegister wordRegister, int offset)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            Debug.Assert(wordRegister.Low != null);
            Debug.Assert(wordRegister.High != null);
            ByteRegister.A.CopyFrom(instruction, wordRegister.Low);
            instruction.WriteLine("\tadi\ta,low " + offset);
            wordRegister.Low.CopyFrom(instruction, ByteRegister.A);
            ByteRegister.A.CopyFrom(instruction, wordRegister.High);
            instruction.WriteLine("\taci\ta,high " + offset);
            wordRegister.High.CopyFrom(instruction, ByteRegister.A);
        }
    }

    public override void SubtractRegister(Instruction instruction, WordRegister wordRegister, int offset)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            Debug.Assert(wordRegister.Low != null);
            Debug.Assert(wordRegister.High != null);
            ByteRegister.A.CopyFrom(instruction, wordRegister.Low);
            instruction.WriteLine("\tsui\ta,low " + -offset);
            wordRegister.Low.CopyFrom(instruction, ByteRegister.A);
            ByteRegister.A.CopyFrom(instruction, wordRegister.High);
            instruction.WriteLine("\tsbi\ta,high " + -offset);
            wordRegister.High.CopyFrom(instruction, ByteRegister.A);
        }
    }
}