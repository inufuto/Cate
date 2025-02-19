﻿namespace Inu.Cate.Sm85;

internal class ByteRegister(int address) : AbstractByteRegister(MinId + address, AddressToName(address))
{
    private const int MinId = 100;
    public readonly int Address = address;
    private static List<Cate.ByteRegister>? registers;

    public static List<Cate.ByteRegister> Registers
    {
        get
        {
            if (registers != null) return registers;
            registers = [];
            for (var address = 0; address < 8; address += 2) {
                registers.Add(new ByteRegister(address + 1));
                registers.Add(new ByteRegister(address));
            }
            return registers;
        }
    }

    private static string AddressToName(int address)
    {
        return Prefix + address;
    }
    public static ByteRegister FromAddress(int address)
    {
        return new ByteRegister(address);
    }





    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmov\t" + this + ",@" + label);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmov\t@" + label + "," + this);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmov\t" + this + ",@" + pointerRegister);
        }
        else {
            if (Equals(pointerRegister, WordRegister.FromAddress(0))) {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.TemporaryRegisters);
                reservation.WordRegister.CopyFrom(instruction, pointerRegister);
                instruction.WriteLine("\tmov\t" + this + "," + offset + "(" + reservation.WordRegister + ")");
            }
            else {
                instruction.WriteLine("\tmov\t" + this + "," + offset + "(" + pointerRegister + ")");
            }
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmov\t" + "@" + pointerRegister + "," + this);
        }
        else {
            if (Equals(pointerRegister, WordRegister.FromAddress(0))) {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.TemporaryRegisters);
                reservation.WordRegister.CopyFrom(instruction, pointerRegister);
                instruction.WriteLine("\tmov\t" + offset + "(" + reservation.WordRegister + ")," + this);
                return;
            }
            instruction.WriteLine("\tmov\t" + offset + "(" + pointerRegister + ")," + this);
        }
    }

    public override Cate.WordRegister? PairRegister => WordRegister.FromAddress(Address & 0xfe);

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        switch (operand) {
            case IntegerOperand integerOperand:
                instruction.WriteLine("\t" + operation + "\t" + this + "," + integerOperand.IntegerValue);
                break;
            case VariableOperand variableOperand:
                if (variableOperand.Variable.Register != null) {
                    instruction.WriteLine("\t" + operation + "\t" + this + "," + variableOperand.Variable.Register);
                }
                else {
                    instruction.WriteLine("\t" + operation + "\t" + this + ",@" + variableOperand.MemoryAddress());
                }
                break;
            case IndirectOperand indirectOperand: {
                    var variableRegister = indirectOperand.Variable.Register;
                    if (variableRegister is WordRegister pointerRegister) {
                        ViaRegister(pointerRegister, indirectOperand.Offset);
                    }
                    else {
                        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers.Where(r=>!Equals(r, WordRegister.FromAddress(0))&& !r.Conflicts(this)).ToList(), operand);
                        reservation.WordRegister.LoadFromMemory(instruction, indirectOperand.Variable, 0);
                        ViaRegister(reservation.WordRegister, indirectOperand.Offset);
                    }
                    break;
                    void ViaRegister(Cate.WordRegister register, int offset)
                    {
                        if (offset == 0) {
                            instruction.WriteLine("\t" + operation + "\t" + this + ",@" + register);
                        }
                        else {
                            if (Equals(register, WordRegister.FromAddress(0))) {
                                using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.TemporaryRegisters);
                                reservation.WordRegister.CopyFrom(instruction, register);
                                instruction.WriteLine("\t" + operation + "\t" + this + "," + offset + "(" + reservation.WordRegister + ")");
                            }
                            else {
                                instruction.WriteLine("\t" + operation + "\t" + this + "," + offset + "(" + register + ")");
                            }
                        }
                    }
                }
            default:
                throw new NotImplementedException();
        }
        if (change) {
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }
        instruction.ResultFlags |= Instruction.Flag.Z;
    }
}