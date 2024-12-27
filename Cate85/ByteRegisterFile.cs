using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class ByteRegisterFile(int id) : AbstractByteRegister(id, IdToName(id))
{
    private static List<Cate.ByteRegister>? registers;
    public const int MinId = 1000;
    public const int Count = 16;

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

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.LoadIndirect(instruction, pointerRegister, offset);
        CopyFrom(instruction, reservation.ByteRegister);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        reservation.ByteRegister.CopyFrom(instruction, this);
        reservation.ByteRegister.StoreIndirect(instruction, pointerRegister, offset);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (operand is IntegerOperand integerOperand) {
            instruction.WriteLine("\t" + operation + "\t" + this + "," + integerOperand.IntegerValue);
        }
        else if (operand is VariableOperand { Variable.Register: not null, Offset: 0 } variableOperand) {
            instruction.WriteLine("\t" + operation + "\t" + this + "," + variableOperand.Variable.Register);
        }
        else if (operand is IndirectOperand { Register: not null, Offset: 0 } indirectOperand) {
            instruction.WriteLine("\t" + operation + "\t" + this + ",@" + indirectOperand.Register);
        }
        else {
            using var reservation = ByteOperation.ReserveAnyRegister(instruction, operand);
            reservation.ByteRegister.Load(instruction, operand);
            instruction.WriteLine("\t" + operation + "\t" + this + "," + reservation.ByteRegister);
        }
        if (change) {
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }
        instruction.ResultFlags |= Instruction.Flag.Z;
    }
}