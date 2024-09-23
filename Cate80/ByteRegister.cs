using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Z80;

internal class ByteRegister : Cate.ByteRegister
{
    public static List<Cate.ByteRegister> Registers = new List<Cate.ByteRegister>();


    public static Cate.ByteRegister FromId(int id)
    {
        var register = Registers.Find(r => r.Id == id);
        return register ?? throw new ArgumentOutOfRangeException();
    }

    public static Cate.ByteRegister FromName(string name)
    {
        var register = Registers.Find(r => r.Name == name);
        return register ?? throw new ArgumentOutOfRangeException();
    }

    private ByteRegister(int id, string name) : base(id, name)
    {
        Registers.Add(this);
    }

    public override Cate.WordRegister? PairRegister => WordRegister.Registers.FirstOrDefault(wordRegister => wordRegister.Name.Contains(Name));


    public static readonly ByteRegister A = new ByteRegister(1, "a");
    public static readonly ByteRegister D = new ByteRegister(2, "d");
    public static readonly ByteRegister E = new ByteRegister(3, "e");
    public static readonly ByteRegister B = new ByteRegister(4, "b");
    public static readonly ByteRegister C = new ByteRegister(5, "c");
    public static readonly ByteRegister H = new ByteRegister(6, "h");
    public static readonly ByteRegister L = new ByteRegister(7, "l");

    public static List<Cate.ByteRegister> Accumulators => new List<Cate.ByteRegister>() { A };
    //public static readonly List<ByteRegister> LowRegisters = new List<ByteRegister>() { C, E, L };

    //public override bool IsLow() => LowRegisters.Contains(this);


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

    public override bool Matches(Register register)
    {
        switch (register) {
            case WordRegister wordRegister:
                if (wordRegister.Contains(this))
                    return true;
                break;
        }
        return base.Matches(register);
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Debug.Assert(Equals(A));
        Instruction.WriteTabs(writer, tabCount);
        if (instruction != null && instruction.IsJump()) {
            writer.WriteLine("\tld\t(@Temporary@Byte),a" + comment);
        }
        else {
            writer.WriteLine("\tpush\taf" + comment);
        }
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Debug.Assert(Equals(A));
        Instruction.WriteTabs(writer, tabCount);
        if (instruction != null && instruction.IsJump()) {
            writer.WriteLine("\tld\ta,(@Temporary@Byte)" + comment);
        }
        else {
            writer.WriteLine("\tpop\taf" + comment);
        }
    }


    public static Cate.ByteRegister TemporaryRegister(Instruction instruction, IEnumerable<Cate.ByteRegister> candidates)
    {
        var register = candidates.First(r => !instruction.IsRegisterReserved(r));
        Debug.Assert(register != null);
        return register;
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tld\t" + Name + "," + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0 && Equals(this, A)) {
            instruction.WriteLine("\txor\ta");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        if (Equals(A)) {
            instruction.WriteLine("\tld\ta,(" + label + ")");
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            return;
        }
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.LoadFromMemory(instruction, label);
            CopyFrom(instruction, A);
        }
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        if (Equals(A)) {
            instruction.WriteLine("\tld\t(" + label + "),a");
            return;
        }

        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.StoreToMemory(instruction, label);
        }
    }

    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        var address = variable.MemoryAddress(offset);
        if (Equals(this, A)) {
            LoadFromMemory(instruction, address);
            return;
        }
        if (instruction.IsRegisterReserved(A) && !instruction.IsRegisterReserved(WordRegister.Hl)) {
            using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
                WordRegister.Hl.LoadConstant(instruction, address);
                LoadIndirect(instruction, PointerRegister.Hl, 0);
            }
            return;
        }
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.LoadFromMemory(instruction, address);
            CopyFrom(instruction, A);
        }
    }

    public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
    {
        var address = variable.MemoryAddress(offset);
        if (Equals(this, A)) {
            instruction.WriteLine("\tld\t(" + address + "),a");
            instruction.SetVariableRegister(variable, offset, this);
            return;
        }
        if (!instruction.IsRegisterReserved(WordRegister.Hl) && !WordRegister.Hl.Contains(this)) {
            using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
                WordRegister.Hl.LoadConstant(instruction, address);
                StoreIndirect(instruction, PointerRegister.Hl, 0);
            }
            return;
        }
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.StoreToMemory(instruction, address);
        }
    }

    //public override void Load(Instruction instruction, Operand sourceOperand)
    //{
    //    switch (sourceOperand) {
    //        case IndirectOperand sourceIndirectOperand: {
    //                var pointer = sourceIndirectOperand.Variable;
    //                var offset = sourceIndirectOperand.Offset;
    //                var register = instruction.GetVariableRegister(pointer, 0);
    //                {
    //                    if (register is WordRegister pointerRegister) {
    //                        LoadIndirect(instruction, pointerRegister, offset);
    //                        return;
    //                    }
    //                }
    //                using var reservation = WordOperation.ReserveAnyRegister(instruction, Z80.WordRegister.Pointers(offset));
    //                reservation.WordRegister.LoadFromMemory(instruction, pointer, 0);
    //                LoadIndirect(instruction, reservation.WordRegister, offset);
    //                return;
    //            }
    //    }
    //    base.Load(instruction, sourceOperand);
    //}

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        void Write(Cate.ByteRegister register)
        {
            if (pointerRegister is IndexRegister) {
                instruction.WriteLine("\tld\t" + register + ",(" + pointerRegister + "+" + offset + ")");
            }
            else {
                instruction.WriteLine("\tld\t" + register + ",(" + pointerRegister + ")");
            }

            instruction.AddChanged(register);
            instruction.RemoveRegisterAssignment(register);
        }

        if (pointerRegister is IndexRegister && pointerRegister.IsOffsetInRange(offset)) {
            Write(this);
            return;
        }
        if (offset == 0) {
            if (Equals(pointerRegister, PointerRegister.Hl) || Equals(this, A)) {
                Write(this);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, A)) {
                Write(A);
                CopyFrom(instruction, A);
            }
            return;
        }

        if (pointerRegister.Conflicts(this)) {
            using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.Registers.Where(r => !Equals(r, pointerRegister)).ToList());
            var temporaryRegister = reservation.PointerRegister;
            temporaryRegister.CopyFrom(instruction, pointerRegister);
            temporaryRegister.TemporaryOffset(instruction, offset, () =>
            {
                LoadIndirect(instruction, (Cate.PointerRegister)temporaryRegister, 0);
            });
            return;
        }

        pointerRegister.TemporaryOffset(instruction, offset, () =>
        {
            LoadIndirect(instruction, pointerRegister, 0);
        });
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        void Write(Cate.ByteRegister register)
        {
            if (pointerRegister is IndexRegister) {
                instruction.WriteLine("\tld\t(" + pointerRegister + "+" + offset + ")," + register);
            }
            else {
                instruction.WriteLine("\tld\t(" + pointerRegister + ")," + register);
            }
        }

        if (pointerRegister is IndexRegister && pointerRegister.IsOffsetInRange(offset)) {
            Write(this);
            return;
        }
        if (offset == 0) {
            if (Equals(pointerRegister, PointerRegister.Hl) || Equals(this, A)) {
                Write(this);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, this);
                Write(A);
            }
            return;
        }
        pointerRegister.TemporaryOffset(instruction, offset, () =>
        {
            StoreIndirect(instruction, pointerRegister, 0);
        });
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
    {
        if (Equals(sourceRegister, this)) return;

        instruction.WriteLine("\tld\t" + this + "," + sourceRegister);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
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
        if (!Equals(this, A))
            throw new NotImplementedException();
        switch (operand) {
            case IntegerOperand integerOperand:
                instruction.WriteLine("\t" + operation + integerOperand.IntegerValue);
                instruction.RemoveRegisterAssignment(A);
                return;
            case VariableOperand variableOperand: {
                var variable = variableOperand.Variable;
                var offset = variableOperand.Offset;
                var register = instruction.GetVariableRegister(variableOperand);
                if (register is ByteRegister byteRegister) {
                    Debug.Assert(offset == 0);
                    instruction.WriteLine("\t" + operation + byteRegister);
                    return;
                }

                using var reservation = PointerOperation.ReserveAnyRegister(instruction,
                    new List<Cate.PointerRegister> { PointerRegister.Hl, PointerRegister.Ix, PointerRegister.Iy });
                reservation.PointerRegister.LoadConstant(instruction, variable.MemoryAddress(offset));
                instruction.WriteLine("\t" + operation + "(" + reservation.PointerRegister + ")");
                return;
            }
            case IndirectOperand indirectOperand: {
                var pointer = indirectOperand.Variable;
                var offset = indirectOperand.Offset;
                {
                    var register = instruction.GetVariableRegister(pointer, 0);
                    if (register is PointerRegister pointerRegister) {
                        OperateAccumulatorIndirect(instruction, operation, pointerRegister, offset);
                        instruction.RemoveRegisterAssignment(this);
                        return;
                    }
                }
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerOperation.RegistersToOffset(offset));
                reservation.PointerRegister.LoadFromMemory(instruction, pointer, 0);
                OperateAccumulatorIndirect(instruction, operation, reservation.PointerRegister, offset);
                return;
            }
        }
        throw new NotImplementedException();
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        throw new NotImplementedException();
    }

    public override void Save(Instruction instruction)
    {
        if (Equals(A)) {
            instruction.WriteLine("\tpush\taf");
            return;
        }
        throw new NotImplementedException();
    }

    public override void Restore(Instruction instruction)
    {
        if (Equals(A)) {
            instruction.WriteLine("\tpop\taf");
            return;
        }
        throw new NotImplementedException();
    }

    private static void OperateAccumulatorIndirect(Instruction instruction, string operation,
        Cate.PointerRegister pointerRegister,
        int offset)
    {
        if (!PointerRegister.IsAddable(pointerRegister)) {
            using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerOperation.RegistersToOffset(offset));
            reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
            OperateAccumulatorIndirect(instruction, operation, reservation.PointerRegister, offset);
            return;
        }
        if (pointerRegister is IndexRegister && pointerRegister.IsOffsetInRange(offset)) {
            instruction.WriteLine("\t" + operation + "(" + pointerRegister + "+" + offset + ")");
            return;
        }
        if (offset == 0 && Equals(pointerRegister, PointerRegister.Hl)) {
            instruction.WriteLine("\t" + operation + "(" + pointerRegister + ")");
            return;
        }
        pointerRegister.TemporaryOffset(instruction, offset, () =>
        {
            OperateAccumulatorIndirect(instruction, operation, pointerRegister, 0);
        });
    }

}