namespace Inu.Cate.Sm85;

internal abstract class AbstractByteRegister(int id, string name) : Cate.ByteRegister(id, name)
{
    protected const string Prefix = "r";

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpush\t" + Name + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpop\t" + Name + comment);
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tpush\t" + Name);
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpop\t" + Name);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0) {
            instruction.WriteLine("\tclr\t" + this);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
    {
        if (Equals(sourceRegister, this)) return;

        instruction.WriteLine("\tmov\t" + this + "," + sourceRegister);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tmov\t" + this + "," + value);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }
}