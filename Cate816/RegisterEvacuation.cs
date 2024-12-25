namespace Inu.Cate.Wdc65816;
internal class RegisterEvacuation : IDisposable
{
    private readonly StreamWriter writer;
    private readonly int tabCount;
    public readonly Dictionary<ModeFlag, int> Flags = new();
    //private readonly Register? assignedRegister;

    public RegisterEvacuation(StreamWriter writer, List<Register> registers, Function function)
    {
        this.writer = writer;
        tabCount = 0;
        Evacuate();
    }

    public RegisterEvacuation(StreamWriter writer, List<Register> registers, Instruction instruction, int tabCount)
    {
        this.writer = writer;
        this.tabCount = tabCount;
        if (registers.Any(r => r is ByteZeroPage or WordZeroPage)) {
            PrepareFlag(ModeFlag.Memory);
            //if (instruction.PreviousInstructions.All(i => i.IsRegisterAssigned(WordRegister.A))) {
            //    //ResetMode(ModeFlag.Memory);
            //    assignedRegister = WordRegister.A;
            //}
            //else if (instruction.PreviousInstructions.All(i => i.IsRegisterAssigned(ByteRegister.A))) {
            //    //SetMode(ModeFlag.Memory);
            //    assignedRegister = ByteRegister.A;
            //}
        }
        Evacuate();
        return;

        void PrepareFlag(ModeFlag flag)
        {
            if (instruction.PreviousInstructions.All(i => i.IsConstantAssigned(flag, flag.Value))) {
                Flags[flag] = flag.Value;
            }
            else if (instruction.PreviousInstructions.All(i => i.IsConstantAssigned(flag, 0))) {
                Flags[flag] = 0;
            }
        }
    }

    public RegisterEvacuation(StreamWriter writer, List<Register> registers, int byteCount)
    {
        this.writer = writer;
        //if (byteCount > 0 && registers.Any(r => r is ByteZeroPage or WordZeroPage)) {
        //    savedSize = byteCount;
        //}
        //if (byteCount == 1 && registers.Contains(WordRegister.A)) {
        //    savedSize = byteCount;
        //}
        tabCount = 0;
        Evacuate();
    }

    private void Evacuate()
    {
        //if (assignedRegister == null) return;
        //var savedFlags = new Dictionary<ModeFlag, int>(Flags);
        //ChangeMode(assignedRegister);
        //Cate.Compiler.Instance.AddExternalName(Compiler.TemporaryWordLabel);
        //WriteLine("\tsta\t<" + Compiler.TemporaryWordLabel);
        //RestoreFlags(savedFlags);
    }

    public void Dispose()
    {
        //if (assignedRegister == null) return;
        //var savedFlags = new Dictionary<ModeFlag, int>(Flags);
        //ChangeMode(assignedRegister);
        //Cate.Compiler.Instance.AddExternalName(Compiler.TemporaryWordLabel);
        //WriteLine("\tlda\t<" + Compiler.TemporaryWordLabel);
        //RestoreFlags(savedFlags);
    }
    //private void WriteLine(string s)
    //{
    //    WriteTabs();
    //    writer.WriteLine(s);
    //}

    private void WriteTabs()
    {
        Instruction.WriteTabs(writer, tabCount);
    }

    public virtual void SetFlag(ModeFlag flag)
    {
        WriteTabs();
        flag.SetBit(writer);
    }

    public virtual void ResetFlag(ModeFlag flag)
    {
        WriteTabs();
        flag.ResetBit(writer);
    }

    public void RestoreFlags(Dictionary<ModeFlag, int> savedFlags)
    {
        foreach (var p in savedFlags) {
            if (Flags.TryGetValue(p.Key, out var value) && value == p.Value) continue;
            if (p.Value != 0) {
                SetFlag(p.Key);
            }
            else {
                ResetFlag(p.Key);
            }
        }
    }

    public void ChangeMode(Register register)
    {
        switch (register) {
            case ByteAccumulator or ByteZeroPage:
                SetMode(ModeFlag.Memory);
                break;
            case WordAccumulator or WordZeroPage:
                ResetMode(ModeFlag.Memory);
                break;
        }
    }

    private void ResetMode(ModeFlag flag)
    {
        if (Flags.TryGetValue(flag, out var value) && value == 0) return;
        Flags[flag] = 0;
        ResetFlag(flag);
    }

    private void SetMode(ModeFlag flag)
    {
        if (Flags.TryGetValue(flag, out var value) && value != 0) return;
        Flags[flag] = flag.Value;
        SetFlag(flag);
    }

    public Dictionary<ModeFlag, int> PrepareFlags(Instruction instruction)
    {
        PrepareFlag(ModeFlag.Memory);
        return Flags;

        void PrepareFlag(ModeFlag flag)
        {
            if (instruction.IsConstantAssigned(flag, flag.Value)) {
                Flags[flag] = flag.Value;
            }
            else if (instruction.IsConstantAssigned(flag, 0)) {
                Flags[flag] = 0;
            }
        }
    }
}
