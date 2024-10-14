﻿using System.Diagnostics;

namespace Inu.Cate.Sm83;

internal class WordRegister(int id, ByteRegister high, ByteRegister low, bool addable)
    : Cate.WordRegister(id, high.Name + low.Name)
{
    public static readonly WordRegister Hl = new(11, ByteRegister.H, ByteRegister.L, true);
    public static readonly WordRegister De = new(12, ByteRegister.D, ByteRegister.E, false);
    public static readonly WordRegister Bc = new(13, ByteRegister.B, ByteRegister.C, false);

    public static readonly List<Cate.WordRegister> Registers = [Hl, De, Bc];

    public override Cate.ByteRegister? Low { get; } = low;
    public override Cate.ByteRegister? High { get; } = high;

    public readonly bool Addable = addable;

    public IEnumerable<Register> ByteRegisters
    {
        get
        {
            Debug.Assert(Low != null);
            Debug.Assert(High != null);
            return [Low, High];
        }
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpush\t" + Name + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpop\t" + Name + comment);
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tpush\t" + Name);
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpop\t" + Name);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tld\t" + this + "," + value);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        Debug.Assert(Low != null && High != null);
        Low.LoadFromMemory(instruction, label);
        High.LoadFromMemory(instruction, label + "+1");
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        Debug.Assert(Low != null && High != null);
        Low.StoreToMemory(instruction, label);
        High.StoreToMemory(instruction, label+"+1");
    }

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        Debug.Assert(Low != null && High != null);
        if (offset == 0) {
            if (Equals(pointerRegister, PointerRegister.Hl)) {
                if (Equals(this, Hl)) {
                    using var reservation =
                        WordOperation.ReserveAnyRegister(instruction, [Bc, De]);
                    var wordRegister = reservation.WordRegister;
                    wordRegister.LoadIndirect(instruction, pointerRegister, offset);
                    CopyFrom(instruction, wordRegister);
                    return;
                }

                Low.LoadIndirect(instruction, pointerRegister, 0);
                instruction.WriteLine("\tinc\t" + pointerRegister);
                High.LoadIndirect(instruction, pointerRegister, 0);
                instruction.WriteLine("\tdec\t" + pointerRegister);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                instruction.WriteLine("\tld\ta,(" + pointerRegister + ")");
                Low.CopyFrom(instruction, ByteRegister.A);
                instruction.WriteLine("\tinc\t" + pointerRegister);
                instruction.WriteLine("\tld\ta,(" + pointerRegister + ")");
                High.CopyFrom(instruction, ByteRegister.A);
                instruction.WriteLine("\tdec\t" + pointerRegister);
                instruction.AddChanged(ByteRegister.A);
                instruction.RemoveRegisterAssignment(ByteRegister.A);
            }
            return;
        }
        if (Math.Abs(offset) > 1) {
            var changed = instruction.IsChanged(pointerRegister);
            pointerRegister.Save(instruction);
            pointerRegister.Add(instruction, offset);
            LoadIndirect(instruction, pointerRegister, 0);
            pointerRegister.Restore(instruction);
            if (!changed) {
                instruction.RemoveChanged(pointerRegister);
            }
            return;
        }
        pointerRegister.Add(instruction, offset);
        LoadIndirect(instruction, pointerRegister, 0);
        if (!Equals(pointerRegister.WordRegister, this)) {
            pointerRegister.Add(instruction, -offset);
        }
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        Debug.Assert(Low != null && High != null);
        if (offset == 0) {
            if (Equals(pointerRegister, PointerRegister.Hl)) {
                if (Equals(this, Hl)) {
                    using var reservation = WordOperation.ReserveAnyRegister(instruction, [Bc, De]);
                    var wordRegister = reservation.WordRegister;
                    wordRegister.CopyFrom(instruction, this);
                    wordRegister.StoreIndirect(instruction, pointerRegister, offset);
                    return;
                }

                Low.StoreIndirect(instruction, pointerRegister, 0);
                instruction.WriteLine("\tinc\t" + pointerRegister);
                High.StoreIndirect(instruction, pointerRegister, 0);
                instruction.WriteLine("\tdec\t" + pointerRegister);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.CopyFrom(instruction, Low);
                instruction.WriteLine("\tld\t(" + pointerRegister + "),a");
                instruction.WriteLine("\tinc\t" + pointerRegister);
                ByteRegister.A.CopyFrom(instruction, High);
                instruction.WriteLine("\tld\t(" + pointerRegister + "),a");
                instruction.WriteLine("\tdec\t" + pointerRegister);
            }
            return;
        }

        if (!instruction.TemporaryRegisters.Contains(pointerRegister)) {
            AddAndStore(pointerRegister);
        }
        else {
            var candidates = PointerRegister.Registers.Where(r => !Equals(r, pointerRegister)).ToList();
            using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
            var temporaryRegister = reservation.PointerRegister;
            temporaryRegister.CopyFrom(instruction, pointerRegister);
            AddAndStore(temporaryRegister);
        }

        return;

        void AddAndStore(Cate.PointerRegister wordRegister)
        {
            wordRegister.Add(instruction, offset);
            StoreIndirect(instruction, wordRegister, 0);
        }
    }

    public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
    {
        if (Equals(this, sourceRegister))
            return;

        Debug.Assert(Low != null && High != null);
        Debug.Assert(sourceRegister is { Low: not null, High: not null });
        Low.CopyFrom(instruction, sourceRegister.Low);
        High.CopyFrom(instruction, sourceRegister.High);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        throw new NotImplementedException();
    }
}