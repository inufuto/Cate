using System.Diagnostics;

namespace Inu.Cate.Wdc65816;

internal class ByteZeroPage(int id) : Cate.ByteRegister(id, IdToName(id))
{
    public const int MinId = 10;
    public const int Count = 16 - 4;

    public static List<Cate.ByteRegister> Registers
    {
        get
        {
            var registers = new List<Cate.ByteRegister>();
            for (var i = 0; i < Count; i++) {
                registers.Add(new ByteZeroPage(MinId + i));
            }
            return registers;
        }
    }


    public static ByteZeroPage FromOffset(int offset)
    {
        return new ByteZeroPage(offset + MinId);
    }

    private static string IdToName(int id)
    {
        var offset = IdToOffset(id);
        return "<" + Compiler.ZeroPageLabel + "+" + offset;
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


    public int Offset => IdToOffset(Id);

    public override Cate.WordRegister? PairRegister => WordZeroPage.FromOffset(Offset);

    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0) {
            ByteOperation.ClearByte(instruction, AsmName);
            instruction.AddChanged(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        using (var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers)) {
            var register = reservation.ByteRegister;
            register.LoadConstant(instruction, value);
            register.StoreToMemory(instruction, Name);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        using (var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers)) {
            var register = reservation.ByteRegister;
            register.LoadFromMemory(instruction, variable, offset);
            register.StoreToMemory(instruction, Name);
            instruction.AddChanged(this);
        }
        instruction.AddChanged(this);
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
    {
        using (var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers)) {
            var register = reservation.ByteRegister;
            register.LoadFromMemory(instruction, Name);
            register.StoreToMemory(instruction, variable, offset);
        }
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            ByteRegister.A.LoadIndirect(instruction, pointerRegister, offset);
            ByteRegister.A.StoreToMemory(instruction, Name);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        var register = reservation.ByteRegister;
        register.LoadFromMemory(instruction, Name);
        register.StoreIndirect(instruction, pointerRegister, offset);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        using (var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers)) {
            var register = reservation.ByteRegister;
            register.LoadFromMemory(instruction, label);
            register.StoreToMemory(instruction, Name);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
        var register = reservation.ByteRegister;
        register.LoadFromMemory(instruction, Name);
        register.StoreToMemory(instruction, label);
        //using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
        //    var register = ByteRegister.A;
        //    register.LoadFromMemory(instruction, Name);
        //    register.StoreToMemory(instruction, label);
        //}
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister register)
    {
        if (register is ByteRegister byteRegister) {
            byteRegister.StoreToMemory(instruction, Name);
        }
        else {
            using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
            var temporaryRegister = reservation.ByteRegister;
            temporaryRegister.CopyFrom(instruction, register);
            temporaryRegister.StoreToMemory(instruction, Name);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        for (var i = 0; i < count; ++i) {
            ModeFlag.Memory.SetBit(instruction);
            instruction.WriteLine("\t" + operation + "\t" + Name);
        }
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            ByteRegister.A.LoadFromMemory(instruction, Name);
            ByteRegister.A.Operate(instruction, operation, change, operand);
            ByteRegister.A.StoreToMemory(instruction, Name);
        }
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            ByteRegister.A.LoadFromMemory(instruction, Name);
            ByteRegister.A.Operate(instruction, operation, change, operand);
            ByteRegister.A.StoreToMemory(instruction, Name);
        }
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Save(Instruction instruction)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            ByteRegister.A.CopyFrom(instruction, this);
            ByteRegister.A.Save(instruction);
        }
    }

    public override void Restore(Instruction instruction)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            ByteRegister.A.Restore(instruction);
            CopyFrom(instruction, ByteRegister.A);
        }
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        //var registerAssigned = instruction != null && instruction.IsRegisterAssigned(ByteRegister.A);
        //if (registerAssigned) {
        //    Cate.Compiler.Instance.AddExternalName("ZB0");
        //    writer.WriteLine("\tsta\t<ZB0");
        //}
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tlda\t" + this + " | pha" + comment);
        //if (registerAssigned) {
        //    writer.WriteLine("\tlda\t<ZB0");
        //}
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        //var registerAssigned = instruction != null && instruction.IsRegisterAssigned(ByteRegister.A);
        //if (registerAssigned) {
        //    Cate.Compiler.Instance.AddExternalName("ZB0");
        //    writer.WriteLine("\tsta\t<ZB0");
        //}
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpla | sta\t" + this + comment);
        //if (registerAssigned) {
        //    writer.WriteLine("\tlda\t<ZB0");
        //}
    }
}