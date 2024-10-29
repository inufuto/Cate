using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class WordRegisterFile(int id) : AbstractWordRegister(id, IdToName(id))
{
    private static List<Cate.WordRegister>? registers;
    public const int MinId = 1500;
    public const int Count = ByteRegisterFile.Count / 2;

    public static IEnumerable<Cate.WordRegister> Registers
    {
        get
        {
            registers = new List<Cate.WordRegister>();
            for (var i = 0; i < Count; i++) {
                registers.Add(new WordRegisterFile(MinId + i));
            }
            return registers;
        }
    }

    private static string IdToName(int id) => Prefix + " " + IdToLabel(id);

    public static string IdToLabel(int id) => Compiler.ZeroPageLabel + "+" + IdToOffset(id);

    private static int IdToOffset(int id)
    {
        Debug.Assert(IsIdInRange(id));
        return (id - MinId) * 2;
    }

    private static bool IsIdInRange(int id)
    {
        return id is >= MinId and < MinId + Count;
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers);
        reservation.WordRegister.LoadFromMemory(instruction, label);
        CopyFrom(instruction, reservation.WordRegister);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers);
        reservation.WordRegister.CopyFrom(instruction, this);
        reservation.WordRegister.StoreToMemory(instruction, label);
    }

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers);
        reservation.WordRegister.LoadIndirect(instruction, pointerRegister, offset);
        CopyFrom(instruction, reservation.WordRegister);
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers);
        reservation.WordRegister.CopyFrom(instruction, this);
        reservation.WordRegister.StoreIndirect(instruction, pointerRegister, offset);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers);
        reservation.WordRegister.CopyFrom(instruction, this);
        reservation.WordRegister.Operate(instruction, operation, change, operand);
        if (change) {
            CopyFrom(instruction, this);
        }
    }
}