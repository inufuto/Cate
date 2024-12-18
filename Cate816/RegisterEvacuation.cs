namespace Inu.Cate.Wdc65816;
internal class RegisterEvacuation : IDisposable
{
    private readonly Compiler compiler;
    private readonly StreamWriter writer;
    private readonly int tabCount;
    private readonly int? savedSize;
    private readonly Dictionary<ModeFlag, int> flags = new();

    public RegisterEvacuation(Compiler compiler, StreamWriter writer, List<Register> registers, Function function)
    {
        this.compiler = compiler;
        this.writer = writer;
        tabCount = 0;
        if (function.Parameters.Count > 0 && registers.Any(r => r is ByteZeroPage or WordZeroPage)) {
            savedSize = function.Parameters[0].Type.ByteCount;
        }
        switch (savedSize) {
            case 1:
                WriteLine("\ta8");
                break;
            case 2:
                WriteLine("\ta16");
                break;
        }
        Evacuate();
    }

    public RegisterEvacuation(Compiler compiler, StreamWriter writer, List<Register> registers, Instruction instruction, int tabCount)
    {
        this.compiler = compiler;
        this.writer = writer;
        this.tabCount = tabCount;
        if (registers.Any(r => r is ByteZeroPage or WordZeroPage)) {
            PrepareFlag(ModeFlag.Memory);
            PrepareFlag(ModeFlag.IndexRegister);
            if (instruction.PreviousInstructions.All(i => i.IsRegisterAssigned(WordRegister.A))) {
                ResetMode(ModeFlag.Memory);
                savedSize = 2;
            }
            else if (instruction.PreviousInstructions.All(i => i.IsRegisterAssigned(ByteRegister.A))) {
                SetMode(ModeFlag.Memory);
                savedSize = 1;
            }
        }
        SetMode();
        Evacuate();
        return;

        void PrepareFlag(ModeFlag flag)
        {
            if (instruction.PreviousInstructions.All(i => i.IsConstantAssigned(flag, flag.Value))) {
                //SetMode(flag);
                flags[flag] = flag.Value;
            }
            else if (instruction.PreviousInstructions.All(i => i.IsConstantAssigned(flag, 0))) {
                //ResetMode(flag);
                flags[flag] = 0;
            }
        }
    }

    public RegisterEvacuation(Compiler compiler, StreamWriter writer, List<Register> registers, int byteCount)
    {
        this.compiler = compiler;
        this.writer = writer;
        if (byteCount > 0 && registers.Any(r => r is ByteZeroPage or WordZeroPage)) {
            savedSize = byteCount;
        }
        tabCount = 0;
        Evacuate();
    }

    private void Evacuate()
    {
        if (savedSize == null) return;
        var savedFlags = new Dictionary<ModeFlag, int>(flags);
        compiler.AddExternalName(Compiler.TemporaryWordLabel);
        WriteLine("\tsta\t<" + Compiler.TemporaryWordLabel);
        RestoreFlags(savedFlags);
    }

    public void Dispose()
    {
        if (savedSize == null) return;
        //var savedFlags = new Dictionary<ModeFlag, int>(flags);
        SetMode();
        WriteLine("\tlda\t<" + Compiler.TemporaryWordLabel);
        //RestoreFlags(savedFlags);
    }
    private void WriteLine(string s)
    {
        WriteTabs();
        writer.WriteLine(s);
    }

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
            if (flags.TryGetValue(p.Key, out var value) && value == p.Value) continue;
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
            case ByteIndexRegister:
                SetMode(ModeFlag.IndexRegister);
                break;
            case WordAccumulator or WordZeroPage:
                ResetMode(ModeFlag.Memory);
                break;
            case WordIndexRegister:
                ResetMode(ModeFlag.IndexRegister);
                break;
                //default:
                //    throw new NotImplementedException();
        }
    }

    private void ResetMode(ModeFlag flag)
    {
        if (flags.TryGetValue(flag, out var value) && value == 0) return;
        flags[flag] = 0;
        ResetFlag(flag);
    }

    private void SetMode(ModeFlag flag)
    {
        if (flags.TryGetValue(flag, out var value) && value != 0) return;
        flags[flag] = flag.Value;
        SetFlag(flag);
    }
    private void SetMode()
    {
        switch (savedSize) {
            case 1:
                SetMode(ModeFlag.Memory);
                break;
            case 2:
                ResetMode(ModeFlag.Memory);
                break;
        }
    }

    public Dictionary<ModeFlag, int> PrepareFlags(Instruction instruction)
    {
        PrepareFlag(ModeFlag.Memory);
        PrepareFlag(ModeFlag.IndexRegister);
        return flags;

        void PrepareFlag(ModeFlag flag)
        {
            if (instruction.IsConstantAssigned(flag, flag.Value)) {
                flags[flag] = flag.Value;
            }
            else if (instruction.IsConstantAssigned(flag, 0)) {
                flags[flag] = 0;
            }
        }
    }
}
