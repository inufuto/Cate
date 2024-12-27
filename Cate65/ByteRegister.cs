using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Mos6502;

internal abstract class ByteRegister(int id, string name) : Cate.ByteRegister(id, name)
{
    public static readonly ByteRegister A = new Accumulator(1, "a");
    public static readonly ByteRegister X = new IndexRegister(2, "x");
    public static readonly ByteRegister Y = new IndexRegister(3, "y");

    public static readonly List<Cate.ByteRegister> Registers = [A, X, Y];

    public static Cate.ByteRegister FromId(int id)
    {
        return Registers.First(r => r.Id == id);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tld" + Name + "\t#" + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
        instruction.ResultFlags |= Instruction.Flag.Z;
    }

    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        instruction.WriteLine("\tld" + Name + "\t" + variable.MemoryAddress(offset));
        instruction.RemoveRegisterAssignment(this);
        instruction.SetVariableRegister(variable, offset, this);
        instruction.AddChanged(this);
        instruction.ResultFlags |= Instruction.Flag.Z;
    }

    public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
    {
        instruction.WriteLine("\tst" + Name + "\t" + variable.MemoryAddress(offset));
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tld" + Name + "\t" + label);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
        instruction.ResultFlags |= Instruction.Flag.Z;
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tst" + Name + "\t" + label);
    }

    public override void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
    {
        Debug.Assert(Equals(A));
        if (pointerRegister.IsOffsetInRange(offset)) {
            Debug.Assert(offset is >= 0 and < 0x100);
            if (pointerRegister is WordZeroPage zeroPage) {
                ViaZeroPage(zeroPage);
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, WordZeroPage.Registers);
                reservation.WordRegister.CopyFrom(instruction, pointerRegister);
                ViaZeroPage((WordZeroPage)reservation.WordRegister);
            }
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(instruction);
            var temporaryRegister = reservation.WordRegister;
            temporaryRegister.CopyFrom(instruction, pointerRegister);
            temporaryRegister.Add(instruction, offset);
            LoadIndirect(instruction, temporaryRegister, 0);
            instruction.RemoveRegisterAssignment(temporaryRegister);
            instruction.AddChanged(temporaryRegister);
        }
        return;

        void ViaZeroPage(WordZeroPage zeroPage)
        {
            using (ByteOperation.ReserveRegister(instruction, Y)) {
                Compiler.Instance.LoadIndirect(instruction, this, zeroPage, offset);
            }
        }
    }

    public override void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
    {
        Debug.Assert(Equals(A));
        if (pointerRegister.IsOffsetInRange(offset)) {
            if (pointerRegister is WordZeroPage zeroPage) {
                ViaZeroPage(zeroPage);
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, WordZeroPage.Registers);
                reservation.WordRegister.CopyFrom(instruction, pointerRegister);
                ViaZeroPage((WordZeroPage)reservation.WordRegister);
            }
        }
        else {
            pointerRegister.Add(instruction, offset);
            StoreIndirect(instruction, pointerRegister, 0);
            instruction.RemoveRegisterAssignment(pointerRegister);
            instruction.AddChanged(pointerRegister);
        }

        return;

        void ViaZeroPage(WordZeroPage zeroPage)
        {
            Compiler.Instance.StoreIndirect(instruction, this, zeroPage, offset);
        }
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister register)
    {
        if (register.Equals(this)) {
            return;
        }
        switch (register) {
            case ByteRegister byteRegister:
                instruction.WriteLine("\tt" + byteRegister + this);
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            case ByteZeroPage zeroPage:
                LoadFromMemory(instruction, zeroPage.Name);
                return;
        }
        throw new NotImplementedException();
    }

    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        if (Equals(A)) {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + Name);
            }
            if (!change)
                return;
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }
        using (var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteZeroPage.Registers)) {
            reservation.ByteRegister.CopyFrom(instruction, this);
            reservation.ByteRegister.Operate(instruction, operation, change, count);
            CopyFrom(instruction, reservation.ByteRegister);
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (operand is VariableOperand variableOperand) {
            var register = instruction.GetVariableRegister(variableOperand);
            switch (register) {
                case ByteZeroPage zeroPage:
                    instruction.WriteLine("\t" + operation + "\t" + zeroPage);
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    if (!change)
                        return;
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
                case ByteRegister byteRegister:
                    using (var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteZeroPage.Registers)) {
                        var temporary = reservation.ByteRegister;
                        temporary.CopyFrom(instruction, byteRegister);
                        instruction.WriteLine("\t" + operation + "\t" + temporary);
                    }
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    if (!change)
                        return;
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
            }
        }
        ByteOperation.Operate(instruction, operation, change, operand);
    }

    public abstract void Decrement(Instruction instruction);
}

internal class Accumulator(int id, string name) : ByteRegister(id, name)
{
    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        instruction.WriteLine("\t" + operation + "\t" + operand);
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }


    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tpha");
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpla");
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        writer.WriteLine("\tpha" + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        writer.WriteLine("\tpla" + comment);
    }

    public override void Decrement(Instruction instruction)
    {
        instruction.WriteLine("\tsec|sbc\t#1");
    }
}

internal class IndexRegister(int id, string name) : ByteRegister(id, name)
{
    public override void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
    {
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.LoadIndirect(instruction, pointerRegister, offset);
            CopyFrom(instruction, A);
        }
    }

    public override void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
    {
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.StoreIndirect(instruction, pointerRegister, offset);
        }
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister register)
    {
        if (register is IndexRegister) {
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, register);
                base.CopyFrom(instruction, A);
            }
            return;
        }
        base.CopyFrom(instruction, register);
    }

    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        switch (operation) {
            case "inc":
                operation = "in" + Name;
                break;
            case "dec":
                operation = "de" + Name;
                break;
            default:
                base.Operate(instruction, operation, change, count);
                return;
        }
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation);
        }
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (operation.StartsWith("cp")) {
            Debug.Assert(!change);
            if (operand is ConstantOperand or VariableOperand) {
                base.Operate(instruction, operation, change, operand);
                return;
            }
            operation = "cmp";
        }
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.Operate(instruction, operation, change, operand);
            if (change) {
                CopyFrom(instruction, A);
            }
        }
    }

    public override void Decrement(Instruction instruction)
    {
        instruction.WriteLine("\tde" + Name);
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        throw new NotImplementedException();
    }

    public override void Save(Instruction instruction)
    {
        Cate.Compiler.Instance.AddExternalName("ZB0");
        instruction.WriteLine("\tsta\t<ZB0");
        instruction.WriteLine("\tt" + Name + "a");
        instruction.WriteLine("\tpha");
        instruction.WriteLine("\tlda\t<ZB0");
    }

    public override void Restore(Instruction instruction)
    {
        Cate.Compiler.Instance.AddExternalName("ZB0");
        instruction.WriteLine("\tsta\t<ZB0");
        instruction.WriteLine("\tpla");
        instruction.WriteLine("\tta" + Name);
        instruction.WriteLine("\tlda\t<ZB0");
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        // cannot save : don't assign to variable
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        // cannot save : don't assign to variable
    }
}