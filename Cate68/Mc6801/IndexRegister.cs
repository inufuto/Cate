using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mc6800.Mc6801;

internal class IndexRegister : Mc6800.IndexRegister
{
    public new static IndexRegister X = new(3, "x");

    private protected IndexRegister(int id, string name) : base(id, name) { }
    public override void CopyFrom(Instruction instruction, WordRegister sourceRegister)
    {
        if (Equals(this, sourceRegister)) return;
        Debug.Assert(Equals(sourceRegister, PairRegister.D));
        instruction.WriteLine("\tstd\t<" + ZeroPage.Word.Label);
        LoadFromMemory(instruction, "<" + ZeroPage.Word.Label);
        instruction.SetRegisterCopy(this, sourceRegister);
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tpsh" + AsmName);
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpul" + AsmName);
    }

    public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpsh" + AsmName);
    }

    public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpul" + AsmName);
    }
}