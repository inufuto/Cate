using System;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mos6502;

internal class PairWordRegister(int id, ByteRegister high, ByteRegister low)
    : Cate.WordRegister(id, high.Name + low.Name)
{
    public static PairWordRegister Xy = new(4, ByteRegister.X, ByteRegister.Y);

    public override Cate.ByteRegister? High => high;
    public override Cate.ByteRegister? Low => low;


    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        low.Save(writer, comment, instruction, tabCount);
        high.Save(writer, "", instruction, tabCount);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        high.Save(writer, comment, instruction, tabCount);
        low.Save(writer, "", instruction, tabCount);
    }


    public override void LoadConstant(Instruction instruction, string value)
    {
        low.LoadConstant(instruction, "low(" + value + ")");
        high.LoadConstant(instruction, "high(" + value + ")");
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        low.LoadFromMemory(instruction, label + "+0");
        high.LoadFromMemory(instruction, label + "+1");
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        low.StoreToMemory(instruction, label + "+0");
        high.StoreToMemory(instruction, label + "+1");
    }

    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        low.LoadFromMemory(instruction, variable, offset);
        high.LoadFromMemory(instruction, variable, offset + 1);
    }

    public override void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
    {
        high.LoadIndirect(instruction, pointerRegister, offset + 1);
        low.LoadIndirect(instruction, pointerRegister, offset);
    }

    public override void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
    {
        low.StoreIndirect(instruction, pointerRegister, offset);
        high.StoreIndirect(instruction, pointerRegister, offset + 1);
    }

    public override void CopyFrom(Instruction instruction, WordRegister sourceRegister)
    {
        if (!sourceRegister.IsPair()) return;
        Debug.Assert(sourceRegister is { Low: { }, High: { } });
        low.CopyFrom(instruction, sourceRegister.Low);
        high.CopyFrom(instruction, sourceRegister.High);
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
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            ByteRegister.A.CopyFrom(instruction, low);
            instruction.WriteLine("\tpha");
            ByteRegister.A.CopyFrom(instruction, high);
            instruction.WriteLine("\tpha");
        }
    }

    public override void Restore(Instruction instruction)
    {
        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
            instruction.WriteLine("\tpla");
            high.CopyFrom(instruction, ByteRegister.A);
            instruction.WriteLine("\tpla");
            low.CopyFrom(instruction, ByteRegister.A);
        }
    }
}