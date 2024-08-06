using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.MuCom87;

internal class ByteRegister : Cate.ByteRegister
{
    public static readonly Accumulator A = new(1);
    public static readonly ByteRegister D = new(2, "d");
    public static readonly ByteRegister E = new(3, "e");
    public static readonly ByteRegister B = new(4, "b");
    public static readonly ByteRegister C = new(5, "c");
    public static readonly ByteRegister H = new(6, "h");
    public static readonly ByteRegister L = new(7, "l");

    public static List<Cate.ByteRegister> Registers = new() { A, D, E, B, C, H, L };

    protected ByteRegister(int id, string name) : base(id, name) { }

    public override bool Conflicts(Register? register)
    {
        switch (register) {
            case WordRegister wordRegister:
                if (wordRegister.Contains(this))
                    return true;
                break;
            case ByteRegister byteRegister:
                if (PairRegister != null && PairRegister.Contains(byteRegister))
                    return true;
                break;
        }
        return base.Conflicts(register);
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Debug.Assert(Equals(A));
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpush\tv" + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Debug.Assert(Equals(A));
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpop\tv" + comment);
    }

    public override Cate.WordRegister? PairRegister => WordRegister.Registers.FirstOrDefault(wordRegister => wordRegister.Contains(this));

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tmvi\t" + AsmName + "," + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0 && Equals(this, A)) {
            instruction.WriteLine("\txra\ta,a");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        var address = variable.MemoryAddress(offset);
        LoadFromMemory(instruction, address);
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmov\t" + Name + "," + label);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
    {
        var address = variable.MemoryAddress(offset);
        StoreToMemory(instruction, address);
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmov\t" + label + "," + Name);
    }

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        if (pointerRegister.Contains(this)) {
            using var reservation = PointerOperation.ReserveAnyRegister(instruction);
            reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
            instruction.AddChanged(reservation.PointerRegister);
            LoadIndirect(instruction, reservation.PointerRegister, offset);
            return;
        }
        if (pointerRegister.IsOffsetInRange(offset)) {
            LoadIndirect(instruction, pointerRegister);
            return;
        }
        pointerRegister.TemporaryOffset(instruction, offset, () => { LoadIndirect(instruction, pointerRegister); });
    }

    public virtual void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister)
    {
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.LoadIndirect(instruction, pointerRegister);
            CopyFrom(instruction, A);
        }
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        if (pointerRegister.IsOffsetInRange(offset)) {
            StoreIndirect(instruction, pointerRegister);
            return;
        }
        pointerRegister.TemporaryOffset(instruction, offset, () =>
        {
            StoreIndirect(instruction, pointerRegister);
        });
    }

    public virtual void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister)
    {
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.StoreIndirect(instruction, pointerRegister);
        }
    }


    public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
    {
        if (Equals(sourceRegister, A)) {
            instruction.WriteLine("\tmov\t" + Name + ",a");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, sourceRegister);
            instruction.WriteLine("\tmov\t" + Name + ",a");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }
    }


    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + Name);
        }
        instruction.AddChanged(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.Operate(instruction, operation, change, operand);
            if (change) {
                CopyFrom(instruction, A);
            }
        }
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.Operate(instruction, operation, change, operand);
            if (change) {
                CopyFrom(instruction, A);
            }
        }
    }

    public override void Save(Instruction instruction)
    {
        A.Save(instruction);
        A.CopyFrom(instruction, this);
    }

    public override void Restore(Instruction instruction)
    {
        CopyFrom(instruction, A);
        A.Restore(instruction);
    }

    public static List<Cate.ByteRegister> RegistersOtherThan(ByteRegister register)
    {
        return Registers.FindAll(r => !Equals(r, register));
    }
}