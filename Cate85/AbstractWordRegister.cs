namespace Inu.Cate.Sm85;

internal abstract class AbstractWordRegister(int id, string name) : Cate.WordRegister(id, name)
{
    protected const string Prefix = "rr";

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tmovw\t" + this + "," + value);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpushw\t" + Name + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpopw\t" + Name + comment);
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tpushw\t" + Name);
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpopw\t" + Name);
    }

    public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
    {
        if (Equals(sourceRegister, this)) return;
        instruction.WriteLine("\tmovw\t" + this + "," + sourceRegister);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (operand is IntegerOperand integerOperand) {
            instruction.WriteLine("\t" + operation + "\t" + this + "," + integerOperand.IntegerValue);
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(instruction, operand);
            reservation.WordRegister.Load(instruction, operand);
            instruction.WriteLine("\t" + operation + "\t" + this + "," + reservation.WordRegister);
        }
        if (change) {
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }
    }
}