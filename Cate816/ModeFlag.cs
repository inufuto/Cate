namespace Inu.Cate.Wdc65816;

internal class ModeFlag(int id, string name, int value, string directive, Action<Instruction>? changeAction) : Register(id, 1, name)
{
    public static ModeFlag Memory = new(8, "mmf", 0x20, "a", null);
    public static ModeFlag IndexRegister = new(9, "imf", 0x10, "i", instruction =>
    {
        instruction.AddChanged(WordRegister.X);
        instruction.AddChanged(WordRegister.Y);
        instruction.RemoveRegisterAssignment(WordRegister.X);
        instruction.RemoveRegisterAssignment(WordRegister.Y);
    });

    private static readonly Dictionary<ModeFlag, int> LastFlags = new();

    public readonly int Value = value;
    public readonly string Directive = directive;


    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        writer.WriteLine("\tphp" + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        writer.WriteLine("\tplp" + comment);
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tphp");
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tplp");
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        throw new NotImplementedException();
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        throw new NotImplementedException();
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        throw new NotImplementedException();
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        throw new NotImplementedException();
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        throw new NotImplementedException();
    }

    public void SetBit(Instruction instruction)
    {
        if (instruction.IsConstantAssigned(this, Value)) {
            if (!LastFlags.TryGetValue(this, out var lastValue) || lastValue != Value) {
                instruction.WriteLine("\t" + Directive + "8");
            }
        }
        else {
            instruction.WriteLine($"\tsep\t#${Value:x} | " + Directive + "8");
            instruction.SetRegisterConstant(this, Value);
            instruction.AddChanged(this);
            changeAction?.Invoke(instruction);
        }

        LastFlags[this] = Value;
    }
    public void ResetBit(Instruction instruction)
    {
        if (instruction.IsConstantAssigned(this, 0)) {
            if (!LastFlags.TryGetValue(this, out var lastValue) || lastValue != 0) {
                instruction.WriteLine("\t" + Directive + "16");
            }
        }
        else {
            instruction.WriteLine($"\trep\t#${Value:x} | " + Directive + "16");
            instruction.SetRegisterConstant(this, 0);
            instruction.AddChanged(this);
        }
        LastFlags[this] = 0;
    }

    public void SetBit(StreamWriter writer)
    {
        writer.WriteLine($"\tsep\t#${Value:x} | " + Directive + "8");
    }

    public void ResetBit(StreamWriter writer)
    {
        writer.WriteLine($"\trep\t#${Value:x} | " + Directive + "16");
    }
}