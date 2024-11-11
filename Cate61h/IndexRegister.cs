namespace Inu.Cate.Hd61700;

internal class IndexRegister : Register
{
    public static readonly IndexRegister Ix = new(100, "ix");
    public static readonly IndexRegister Iz = new(102, "iz");

    public static List<IndexRegister> Registers(bool constant)
    {
        return constant ? [Ix, Iz] : [Iz, Ix];
    }

    public static string OffsetValue(int offset)
    {
        return offset switch
        {
            0 => "+$sx",
            1 => "+$sy",
            < 0 => "-" + -offset,
            _ => "+" + offset
        };
    }
    private IndexRegister(int id, string name) : base(id, 2, name) { }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    { }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    { }

    public override void Save(Instruction instruction)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction);
        var wordRegister = reservation.WordRegister;
        instruction.WriteLine("\tgre " + AsmName + "," + wordRegister.AsmName);
        wordRegister.Save(instruction);
    }

    public override void Restore(Instruction instruction)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction);
        var wordRegister = reservation.WordRegister;
        wordRegister.Restore(instruction);
        instruction.WriteLine("\tpre " + AsmName + "," + wordRegister.AsmName);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tpre " + AsmName + "," + value);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction);
        var wordRegister = reservation.WordRegister;
        wordRegister.LoadFromMemory(instruction, label);
        FromWordRegister(instruction, wordRegister);
    }

    public void FromWordRegister(Instruction instruction, Cate.WordRegister wordRegister)
    {
        if (instruction.IsRegisterCopy(this, wordRegister)) return;
        instruction.WriteLine("\tpre " + AsmName + "," + wordRegister.AsmName);
        instruction.AddChanged(this);
        instruction.SetRegisterCopy(this, wordRegister);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction);
        var wordRegister = reservation.WordRegister;
        ToWordRegister(instruction, wordRegister);
        wordRegister.StoreToMemory(instruction, label);
    }

    private void ToWordRegister(Instruction instruction, Cate.WordRegister wordRegister)
    {
        instruction.WriteLine("\tgre " + AsmName + "," + wordRegister.AsmName);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction);
        var wordRegister = reservation.WordRegister;
        wordRegister.LoadIndirect(instruction, pointerRegister, offset);
        FromWordRegister(instruction, wordRegister);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction);
        var wordRegister = reservation.WordRegister;
        ToWordRegister(instruction, wordRegister);
        wordRegister.StoreIndirect(instruction, pointerRegister, offset);
    }


    public void LoadConstant(Instruction instruction, Variable variable, int offset)
    {
        if (!instruction.IsConstantAssigned(this, variable, offset)) {
            LoadConstant(instruction, variable.MemoryAddress(offset));
            instruction.SetRegisterConstant(this, new PointerType(variable.Type), variable, offset);
        }
    }
}