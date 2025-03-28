﻿using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class WordRegister(int address) : AbstractWordRegister(MinId + address, AddressToName(address))
{
    private const int MinId = 200;
    public readonly int Address = address;
    private static List<Cate.WordRegister>? registers;

    public static List<Cate.WordRegister> Registers
    {
        get
        {
            if (registers != null) return registers;
            registers = [];
            for (var address = 0; address < 16; address += 2) {
                registers.Add(new WordRegister(address));
            }
            return registers;
        }
    }

    private static string AddressToName(int address)
    {
        return Prefix + address;
    }

    public static WordRegister FromAddress(int address)
    {
        return new WordRegister(address);
    }


    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0) {
            instruction.WriteLine("\txorw\t" + this + "," + this);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override bool IsOffsetInRange(int offset)
    {
        if (offset == 0) return true;
        return Address != 0;
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmovw\t" + this + ",@" + label);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmovw\t@" + label + "," + this);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmovw\t" + this + ",@" + pointerRegister);
        }
        else {
            if (((WordRegister)pointerRegister).Address == 0) {
                var candidates = WordRegister.TemporaryRegisters.Where(r => !Equals(r, this)).ToList();
                using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
                reservation.WordRegister.CopyFrom(instruction, pointerRegister);
                LoadIndirect(instruction, reservation.WordRegister, offset);
                return;
            }
            instruction.WriteLine("\tmovw\t" + this + "," + offset + "(" + pointerRegister + ")");
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmovw\t" + "@" + pointerRegister + "," + this);
        }
        else {
            instruction.WriteLine("\tmovw\t" + offset + "(" + pointerRegister + ")," + this);
        }
    }

    public override Cate.ByteRegister? Low => ByteRegister.FromAddress(Address + 1);
    public override Cate.ByteRegister? High => ByteRegister.FromAddress(Address);
    public IEnumerable<Register> ByteRegisters
    {
        get
        {
            Debug.Assert(Low != null);
            Debug.Assert(High != null);
            return [Low, High];
        }
    }

    public static List<Cate.WordRegister> TemporaryRegisters
    {
        get
        {
            var temporaryRegisters = new List<Cate.WordRegister>();
            for (var address = 8; address < 16; address += 2) {
                temporaryRegisters.Add(new WordRegister(address));
            }
            return temporaryRegisters;
        }
    }
}