using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.I8086;

internal enum SegmentRegister
{
    Es, Cs, Ss, Ds
}

internal abstract class WordRegister : Cate.WordRegister
{
    public static List<Cate.WordRegister> Registers = new();

    public static WordRegister Ax = new PairRegister(11, "ax", ByteRegister.Ah, ByteRegister.Al);
    public static WordRegister Dx = new PairRegister(13, "dx", ByteRegister.Dh, ByteRegister.Dl);
    public static WordRegister Cx = new PairRegister(12, "cx", ByteRegister.Ch, ByteRegister.Cl);
    public static WordRegister Bx = new PairRegister(14, "bx", ByteRegister.Bh, ByteRegister.Bl);
    public static WordRegister Si = new IndexRegister(17, "si");
    public static WordRegister Di = new IndexRegister(18, "di");
    public static WordRegister Bp = new IndexRegister(19, "bp", SegmentRegister.Ss);

    public abstract IEnumerable<Register> ByteRegisters { get; }
    public static List<Cate.WordRegister> PointerRegisters => [Bx, Si, Di, Bp];

    public readonly SegmentRegister? DefaultSegmentRegister;

    public static PairRegister? FromName(string name)
    {
        foreach (var register in Registers) {
            if (register is PairRegister pairRegister && pairRegister.Name.Equals(name)) return pairRegister;
        }
        return null;
    }

    protected WordRegister(int id, string name, SegmentRegister? defaultSegmentRegister = null) : base(id, name)
    {
        Registers.Add(this);
        DefaultSegmentRegister = defaultSegmentRegister;
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tmov " + this + "," + value);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmov " + this + ",[" + label + "]");
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmov [" + label + "]," + this);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
        instruction.WriteLine("\tmov " + this + ",[" + pointerRegister.AsPointer() + addition + "]");
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
        instruction.WriteLine("\tmov [" + pointerRegister.AsPointer() + addition + "]," + this);
    }

    public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
    {
        if (Equals(this, sourceRegister)) return;
        instruction.WriteLine("\tmov " + this + "," + sourceRegister);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        switch (operand) {
            case ConstantOperand constantOperand:
                instruction.WriteLine("\t" + operation + this + "," + constantOperand.MemoryAddress());
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
                return;
            case VariableOperand variableOperand: {
                    var sourceVariable = variableOperand.Variable;
                    var sourceOffset = variableOperand.Offset;
                    if (sourceVariable.Register is WordRegister sourceRegister) {
                        Debug.Assert(sourceOffset == 0);
                        instruction.WriteLine("\t" + operation + this + "," + sourceRegister);
                        if (change) {
                            instruction.AddChanged(this);
                            instruction.RemoveRegisterAssignment(this);
                        }
                        return;
                    }
                    instruction.WriteLine("\t" + operation + this + ",[" + variableOperand.MemoryAddress() + "]");
                    if (change) {
                        instruction.AddChanged(this);
                        instruction.RemoveRegisterAssignment(this);
                    }
                    return;
                }
        }
        if (operand is not IndirectOperand indirectOperand) throw new NotImplementedException();
        var pointer = indirectOperand.Variable;
        var offset = indirectOperand.Offset;
        if (pointer.Register is WordRegister pointerRegister) {
            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
            instruction.WriteLine("\t" + operation + this + ",[" + pointerRegister.AsPointer() + addition + "]");
            return;
        }
        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.PointerRegisters.Where(r => !r.Conflicts(this)).ToList());
        var temporaryRegister = reservation.WordRegister;
        temporaryRegister.Load(instruction, operand);
        instruction.WriteLine("\t" + operation + this + "," + temporaryRegister);
    }

    public override void Add(Instruction instruction, int offset)
    {
        switch (offset) {
            case 0:
                return;
            case 1:
                instruction.WriteLine("\tinc " + this);
                return;
            case -1:
                instruction.WriteLine("\tdec " + this);
                return;
        }
        if (offset > 0) {
            instruction.WriteLine("\tadd " + this + "," + offset);
            return;
        }
        instruction.WriteLine("\tsub " + this + "," + (-offset));
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpush " + this + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpop " + this + comment);
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tpush " + this);
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpop " + this);
    }
}

public static class WordRegisterExtension
{
    public static string AsPointer(this Cate.WordRegister pointerRegister)
    {
        var defaultSegmentRegister = ((WordRegister)pointerRegister).DefaultSegmentRegister;
        return defaultSegmentRegister is null or SegmentRegister.Ds ? pointerRegister.ToString() : "ds:" + pointerRegister;
    }

}

internal class PairRegister : WordRegister
{
    public readonly ByteRegister HighByteRegister;
    public readonly ByteRegister LowByteRegister;

    public PairRegister(int id, string name, ByteRegister highByteRegister, ByteRegister lowByteRegister,
        SegmentRegister? defaultSegmentRegister = null) : base(id, name)
    {
        HighByteRegister = highByteRegister;
        LowByteRegister = lowByteRegister;
    }

    public override bool IsPair() => true;
    public override bool IsOffsetInRange(int offset) => Equals(Bx);

    public override Cate.ByteRegister? High => HighByteRegister;
    public override Cate.ByteRegister? Low => LowByteRegister;

    public override IEnumerable<Register> ByteRegisters => new[] { HighByteRegister, LowByteRegister };

}

internal class IndexRegister(int id, string name, SegmentRegister? defaultSegmentRegister = SegmentRegister.Ds)
    : WordRegister(id, name, defaultSegmentRegister)
{
    public override bool IsPair() => false;
    public override bool IsOffsetInRange(int offset) => true;

    public override IEnumerable<Register> ByteRegisters => Array.Empty<ByteRegister>();
}