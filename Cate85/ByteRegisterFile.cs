using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class ByteRegisterFile(int id) : AbstractByteRegister(id, IdToName(id))
{
    private static List<Cate.ByteRegister>? registers;
    public const int MinId = 1000;
    public const int Count = 16 - 2;

    public static IEnumerable<Cate.ByteRegister> Registers
    {
        get
        {
            if (registers != null) return registers;
            registers = [];
            for (var i = 0; i < Count; i++) {
                registers.Add(new ByteRegisterFile(MinId + i));
            }
            return registers;
        }
    }

    private static string IdToName(int id)
    {
        var offset = IdToOffset(id);
        return Prefix + " " + Compiler.ZeroPageLabel + "+" + offset;
    }

    private static int IdToOffset(int id)
    {
        Debug.Assert(IsIdInRange(id));
        return id - MinId;
    }

    private static bool IsIdInRange(int id)
    {
        return id is >= MinId and < MinId + Count;
    }


    public override void LoadFromMemory(Instruction instruction, string label)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.LoadFromMemory(instruction, label);
        CopyFrom(instruction, reservation.ByteRegister);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.CopyFrom(instruction, this);
        reservation.ByteRegister.StoreToMemory(instruction, label);
    }

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.LoadIndirect(instruction, pointerRegister, offset);
        CopyFrom(instruction, reservation.ByteRegister);
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.CopyFrom(instruction, this);
        reservation.ByteRegister.StoreIndirect(instruction, pointerRegister, offset);
    }

    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.CopyFrom(instruction, this);
        reservation.ByteRegister.Operate(instruction, operation, change, count);
        if (change) {
            CopyFrom(instruction, reservation.ByteRegister);
        }
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.CopyFrom(instruction, this);
        reservation.ByteRegister.Operate(instruction, operation, change, operand);
        if (change) {
            CopyFrom(instruction, reservation.ByteRegister);
        }
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.CopyFrom(instruction, this);
        reservation.ByteRegister.Operate(instruction, operation, change, operand);
        if (change) {
            CopyFrom(instruction, reservation.ByteRegister);
        }
    }
}