using System.Diagnostics;

namespace Inu.Cate.Wdc65816;

internal class WordZeroPage(int id) : Cate.WordRegister(id, IdToName(id))
{
    public const int MinId = 30;
    public const int Count = ByteZeroPage.Count / 2;

    public static List<Cate.WordRegister> Registers
    {
        get
        {
            var registers = new List<Cate.WordRegister>();
            for (var i = 0; i < Count; i++) {
                registers.Add(new WordZeroPage(MinId + i));
            }
            return registers;
        }
    }
    public static Cate.WordRegister? FromOffset(int offset)
    {
        return new WordZeroPage(offset / 2 + MinId);
    }

    private static string IdToName(int id) => "<" + IdToLabel(id);

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


    public int Offset => IdToOffset(Id);

    public override Cate.ByteRegister? Low => ByteZeroPage.FromOffset(Offset);
    public override Cate.ByteRegister? High => ByteZeroPage.FromOffset(Offset + 1);

    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0) {
            ModeFlag.Memory.ResetBit(instruction);
            instruction.WriteLine("\tstz\t" + AsmName);
            instruction.AddChanged(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        using (var reservation = WordOperation.ReserveAnyRegister(instruction, (List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y])) {
            var register = reservation.WordRegister;
            register.LoadConstant(instruction, value);
            register.StoreToMemory(instruction, Name);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }


    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        using (var reservation = WordOperation.ReserveAnyRegister(instruction, (List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y])) {
            var register = reservation.WordRegister;
            register.LoadFromMemory(instruction, variable, offset);
            register.StoreToMemory(instruction, Name);
            instruction.AddChanged(this);
        }
        instruction.AddChanged(this);
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        using (var reservation = WordOperation.ReserveAnyRegister(instruction, (List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y])) {
            var register = reservation.WordRegister;
            register.LoadFromMemory(instruction, label);
            register.StoreToMemory(instruction, Name);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    //public override void Store(Instruction instruction, AssignableOperand operand)
    //{
    //    if (operand is VariableOperand variableOperand && Equals(variableOperand.Register, this) &&
    //        variableOperand.Offset == 0) {
    //        return;
    //    }

    //    using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
    //        Debug.Assert(Low != null && High != null);
    //        ByteRegister.A.CopyFrom(instruction, Low);
    //        ByteRegister.A.Store(instruction, Cate.Compiler.Instance.LowByteOperand(operand));
    //        ByteRegister.A.CopyFrom(instruction, High);
    //        ByteRegister.A.Store(instruction, Cate.Compiler.Instance.HighByteOperand(operand));
    //    }
    //    //Low.Store(instruction, Cate.Compiler.Instance.LowByteOperand(operand));
    //    //High.Store(instruction, Cate.Compiler.Instance.HighByteOperand(operand));
    //    instruction.SetVariableRegister(operand, this);
    //}

    public override void StoreToMemory(Instruction instruction, string label)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, (List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y]);
        var register = reservation.WordRegister;
        register.LoadFromMemory(instruction, Name);
        register.StoreToMemory(instruction, label);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        using (var reservation = WordOperation.ReserveAnyRegister(instruction, (List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y])) {
            var register = reservation.WordRegister;
            register.LoadIndirect(instruction, pointerRegister, offset);
            register.StoreToMemory(instruction, Name);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, (List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y]);
        var register = reservation.WordRegister;
        register.LoadFromMemory(instruction, Name);
        register.StoreIndirect(instruction, pointerRegister, offset);
    }

    public override void CopyFrom(Instruction instruction, Cate.WordRegister register)
    {
        if (register is WordRegister wordRegister) {
            wordRegister.StoreToMemory(instruction, Name);
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(instruction, (List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y]);
            var temporaryRegister = reservation.WordRegister;
            temporaryRegister.CopyFrom(instruction, register);
            temporaryRegister.StoreToMemory(instruction, Name);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        using (WordOperation.ReserveRegister(instruction, WordRegister.A)) {
            WordRegister.A.LoadFromMemory(instruction, Name);
            WordRegister.A.Operate(instruction, operation, change, operand);
            WordRegister.A.StoreToMemory(instruction, Name);
        }
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override bool IsOffsetInRange(int offset) => true;

    public override void Add(Instruction instruction, int offset)
    {
        using (WordOperation.ReserveRegister(instruction, WordRegister.A)) {
            WordRegister.A.CopyFrom(instruction, this);
            WordRegister.A.Operate(instruction, "clc|adc", true, "#low " + offset);
            CopyFrom(instruction, WordRegister.A);
        }
    }

    public override void Save(Instruction instruction)
    {
        using (WordOperation.ReserveRegister(instruction, WordRegister.A)) {
            WordRegister.A.CopyFrom(instruction, this);
            WordRegister.A.Save(instruction);
        }
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        //var registerAssigned = instruction != null && instruction.IsRegisterAssigned(ByteRegister.A);
        //if (registerAssigned) {
        //    Cate.Compiler.Instance.AddExternalName("ZW0");
        //    writer.WriteLine("\tsta\t<ZW0");
        //}
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tlda\t" + this + " | pha" + comment);
        //if (registerAssigned) {
        //    writer.WriteLine("\tlda\t<ZW0");
        //}
    }

    public override void Restore(Instruction instruction)
    {
        using (WordOperation.ReserveRegister(instruction, WordRegister.A)) {
            WordRegister.A.Restore(instruction);
            CopyFrom(instruction, WordRegister.A);
        }
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        //var registerAssigned = instruction != null && instruction.IsRegisterAssigned(ByteRegister.A);
        //if (registerAssigned) {
        //    Cate.Compiler.Instance.AddExternalName("ZW0");
        //    writer.WriteLine("\tsta\t<ZW0");
        //}
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpla | sta\t" + this + comment);
        //if (registerAssigned) {
        //    writer.WriteLine("\tlda\t<ZW0");
        //}
    }
}