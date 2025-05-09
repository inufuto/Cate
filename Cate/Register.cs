﻿using System;
using System.IO;
using System.Linq;

namespace Inu.Cate;

public abstract class Register(int id, int byteCount, string name) : IComparable<Register>
{
    public readonly int Id = id;
    public readonly int ByteCount = byteCount;
    public readonly string Name = name;

    public override string ToString() => Name;
    public virtual string AsmName => Name;

    protected static ByteOperation ByteOperation => Compiler.Instance.ByteOperation;
    protected static WordOperation WordOperation => Compiler.Instance.WordOperation;
    public int CompareTo(Register? other)
    {
        return other != null ? Id.CompareTo(other.Id) : int.MaxValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is Register register && register.Id == Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public virtual bool Conflicts(Register? register)
    {
        return Equals(register, this);
        //return Equals(register, this) || register is ByteRegister byteRegister && Contains(byteRegister);
    }

    public virtual bool Matches(Register register)
    {
        return Equals(register, this);
    }

    public abstract void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount);

    public abstract void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount);
    public abstract void Save(Instruction instruction);
    public abstract void Restore(Instruction instruction);

    public virtual void LoadConstant(Instruction instruction, int value)
    {
        LoadConstant(instruction, value.ToString());
    }

    public abstract void LoadConstant(Instruction instruction, string value);

    public abstract void LoadFromMemory(Instruction instruction, string label);
    public abstract void StoreToMemory(Instruction instruction, string label);

    public virtual void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        LoadFromMemory(instruction, variable.MemoryAddress(offset));
        instruction.SetVariableRegister(variable, offset, this);
        instruction.AddChanged(this);
    }

    public virtual void StoreToMemory(Instruction instruction, Variable variable, int offset)
    {
        StoreToMemory(instruction, variable.MemoryAddress(offset));
        instruction.SetVariableRegister(variable, offset, this);
    }

    public abstract void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset);

    public abstract void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset);

    public virtual void LoadIndirect(Instruction instruction, Variable pointer, int offset)
    {
        if (pointer.Register is WordRegister pointerRegister) {
            LoadIndirect(instruction, pointerRegister, offset);
            return;
        }
        var allCandidates = WordOperation.PointerRegisters.Where(r => !r.Conflicts(this)).ToList();
        var unReserved = allCandidates.Where(r => !instruction.IsRegisterReserved(r)).ToList();
        var candidates = unReserved.Where(r => r.IsOffsetInRange(offset)).ToList();
        if (candidates.Count == 0) {
            candidates = unReserved;
        }
        if (candidates.Count == 0) {
            candidates = allCandidates;
        }
        if (candidates.Count == 0) {
            candidates = WordOperation.Registers;
        }
        using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
        reservation.WordRegister.LoadFromMemory(instruction, pointer, 0);
        LoadIndirect(instruction, reservation.WordRegister, offset);
    }
    public virtual void StoreIndirect(Instruction instruction, Variable pointer, int offset)
    {
        var register = instruction.GetVariableRegister(pointer, 0, r => r is WordRegister p && p.IsOffsetInRange(offset)) ??
                       instruction.GetVariableRegister(pointer, 0, r => (r is WordRegister p && p.IsOffsetInRange(0)) || r is WordRegister);
        if (register is WordRegister pointerRegister ) {
            StoreIndirect(instruction, pointerRegister, offset);
            return;
        }

        var pointerRegisters = WordOperation.RegistersToOffset(offset);
        if (pointerRegisters.Count == 0) {
            pointerRegisters = WordOperation.Registers;
        }
        var reservation = WordOperation.ReserveAnyRegister(instruction, pointerRegisters);
        reservation.WordRegister.LoadFromMemory(instruction, pointer, 0);
        StoreIndirect(instruction, reservation.WordRegister, offset);
    }
}