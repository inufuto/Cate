using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mos6502;

internal class WordZeroPage : Cate.WordRegister
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
    public static WordRegister? FromOffset(int offset)
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


    public WordZeroPage(int id) : base(id, IdToName(id))
    { }

    public int Offset => IdToOffset(Id);

    public override Cate.ByteRegister? Low => ByteZeroPage.FromOffset(Offset);
    public override Cate.ByteRegister? High => ByteZeroPage.FromOffset(Offset + 1);
    public string Label => IdToLabel(Id);

    public static Register First = new WordZeroPage(MinId);

    public override void LoadConstant(Instruction instruction, string value)
    {
        Debug.Assert(Low != null && High != null);
        Low.LoadConstant(instruction, "low " + value);
        High.LoadConstant(instruction, "high " + value);
    }


    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        Debug.Assert(Low != null && High != null);
        Low.LoadFromMemory(instruction, variable.MemoryAddress(offset));
        High.LoadFromMemory(instruction, variable.MemoryAddress(offset + 1));
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        Debug.Assert(Low != null && High != null);
        Low.LoadFromMemory(instruction, label + "+0");
        High.LoadFromMemory(instruction, label + "+1");
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Store(Instruction instruction, AssignableOperand operand)
    {
        if (operand is VariableOperand variableOperand && Equals(variableOperand.Register, this) &&
            variableOperand.Offset == 0) {
            return;
        }

        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A))
        {
            Debug.Assert(Low != null && High != null);
            ByteRegister.A.CopyFrom(instruction, Low);
            ByteRegister.A.Store(instruction, Cate.Compiler.Instance.LowByteOperand(operand));
            ByteRegister.A.CopyFrom(instruction, High);
            ByteRegister.A.Store(instruction, Cate.Compiler.Instance.HighByteOperand(operand));
        }
        //Low.Store(instruction, Cate.Compiler.Instance.LowByteOperand(operand));
        //High.Store(instruction, Cate.Compiler.Instance.HighByteOperand(operand));
        instruction.SetVariableRegister(operand, this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        Debug.Assert(Low != null && High != null);
        Low.StoreToMemory(instruction, label + "+0");
        High.StoreToMemory(instruction, label + "+1");
    }

    public override void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
    {
        Debug.Assert(Low != null && High != null);
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A))
        {
            ByteRegister.A.LoadIndirect(instruction, pointerRegister, offset);
            Low.CopyFrom(instruction, ByteRegister.A);
            ByteRegister.A.LoadIndirect(instruction, pointerRegister, offset+1);
            High.CopyFrom(instruction, ByteRegister.A);
        }
        //instruction.AddChanged(this);
        //instruction.RemoveVariableRegister(this);
    }

    public override void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
    {
        Debug.Assert(Low != null && High != null);
        Low.StoreIndirect(instruction, pointerRegister, offset);
        High.StoreIndirect(instruction, pointerRegister, offset + 1);
    }

    public override void CopyFrom(Instruction instruction, WordRegister register)
    {
        //if (register is not WordZeroPage zeroPage)
        //    throw new NotImplementedException();
        Debug.Assert(Low != null && High != null);
        Debug.Assert(register is { Low: { }, High: { } });
        Low.CopyFrom(instruction, register.Low);
        High.CopyFrom(instruction, register.High);
        //instruction.AddChanged(this);
        //instruction.RemoveVariableRegister(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        // Must be operated in bytes
        throw new NotImplementedException();
    }

    public override bool IsOffsetInRange(int offset)
    {
        return offset is >= 0 and < 0x100;
    }

    public override void Add(Instruction instruction, int offset)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            Debug.Assert(Low != null && High != null);
            ByteRegister.A.CopyFrom(instruction, Low);
            ByteRegister.A.Operate(instruction, "clc|adc", true, "#low " + offset);
            Low.CopyFrom(instruction, ByteRegister.A);
            ByteRegister.A.CopyFrom(instruction, High);
            ByteRegister.A.Operate(instruction, "adc", true, "#high " + offset);
            High.CopyFrom(instruction, ByteRegister.A);
        }
    }

    public override void Save(Instruction instruction)
    {
        Debug.Assert(Low != null && High != null);
        Low.Save(instruction);
        High.Save(instruction);
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        var registerAssigned = instruction != null && instruction.IsRegisterAssigned(ByteRegister.A);
        if (registerAssigned) {
            Cate.Compiler.Instance.AddExternalName("ZB0");
            writer.WriteLine("\tsta\t<ZB0");
        }
        Debug.Assert(Low != null && High != null);
        Low.Save(writer, comment, null, tabCount);
        High.Save(writer, comment, null, tabCount);
        if (registerAssigned) {
            writer.WriteLine("\tlda\t<ZB0");
        }
    }

    public override void Restore(Instruction instruction)
    {
        Debug.Assert(Low != null && High != null);
        High.Restore(instruction);
        Low.Restore(instruction);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        var registerAssigned = instruction != null && instruction.IsRegisterAssigned(ByteRegister.A);
        if (registerAssigned) {
            Cate.Compiler.Instance.AddExternalName("ZB0");
            writer.WriteLine("\tsta\t<ZB0");
        }
        Debug.Assert(Low != null && High != null);
        High.Restore(writer, comment, null, tabCount);
        Low.Restore(writer, comment, null, tabCount);
        if (registerAssigned) {
            writer.WriteLine("\tlda\t<ZB0");
        }
    }
}