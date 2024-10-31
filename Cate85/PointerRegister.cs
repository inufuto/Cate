namespace Inu.Cate.Sm85;

internal class PointerRegister(AbstractWordRegister wordRegister) : WordPointerRegister(2, wordRegister)
{
    private static List<Cate.PointerRegister>? registers;

    public static List<Cate.PointerRegister> Registers
    {
        get
        {
            if (registers != null) return registers;
            registers = [];
            foreach (var wordRegister in Sm85.WordRegister.Registers) {
                registers.Add(new PointerRegister((AbstractWordRegister)wordRegister));
            }
            foreach (var wordRegister in WordRegisterFile.Registers) {
                registers.Add(new PointerRegister((AbstractWordRegister)wordRegister));
            }
            return registers;
        }
    }

    public static List<Cate.PointerRegister> TemporaryRegisters
    {
        get
        {
            var temporaryRegisters = new List<Cate.PointerRegister>();
            for (var address = 8; address < 16; address += 2) {
                temporaryRegisters.Add(new PointerRegister(new WordRegister(address)));
            }
            return temporaryRegisters;
        }
    }

    public static PointerRegister FromAddress(int address)
    {
        return new PointerRegister((AbstractWordRegister)Sm85.WordRegister.FromAddress(address));
    }

    public override bool IsOffsetInRange(int offset)
    {
        if (offset == 0) return true;
        return WordRegister is not WordRegister { Address: 0 };
    }

    public override void Add(Instruction instruction, int offset)
    {
        throw new NotImplementedException();
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        WordRegister.Operate(instruction, operation, change, operand);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        WordRegister.LoadConstant(instruction, value);
    }
}