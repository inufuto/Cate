using System.Diagnostics;

namespace Inu.Cate.Tlcs900;

internal class WordRegister(int id, string name, Cate.ByteRegister high, Cate.ByteRegister low)
    : Cate.WordRegister(id, name)
{
    public static readonly WordRegister WA = new(21, "wa", ByteRegister.W, ByteRegister.A);
    public static readonly WordRegister BC = new(22, "bc", ByteRegister.B, ByteRegister.C);
    public static readonly WordRegister DE = new(23, "de", ByteRegister.D, ByteRegister.E);
    public static readonly WordRegister HL = new(24, "hl", ByteRegister.H, ByteRegister.L);
    public static readonly WordRegister IX = new(25, "ix", ByteRegister.IXH, ByteRegister.IXL);
    public static readonly WordRegister IY = new(26, "iy", ByteRegister.IYH, ByteRegister.IYL);
    public static readonly WordRegister IZ = new(27, "iz", ByteRegister.IZH, ByteRegister.IZL);
    public static List<WordRegister> All = [WA, BC, DE, HL, IX, IY, IZ];


    public override Cate.ByteRegister High { get; } = high;
    public override Cate.ByteRegister Low { get; } = low;

    internal List<Cate.ByteRegister> ByteRegisters => [Low, High];

    public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
    {
        if (Equals(sourceRegister, this)) return;

        instruction.WriteLine("\tld " + this + "," + sourceRegister);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        switch (operand) {
            case IntegerOperand integerOperand: {
                    instruction.WriteLine("\t" + operation + " " + AsmName + "," + integerOperand.IntegerValue);
                    break;
                }
            case PointerOperand pointerOperand: {
                    instruction.WriteLine("\t" + operation + " " + AsmName + "," + pointerOperand.MemoryAddress());
                    break;
                }
            case VariableOperand variableOperand: {
                    var variable = variableOperand.Variable;
                    var offset = variableOperand.Offset;
                    var register = instruction.GetVariableRegister(variableOperand);
                    if (register is WordRegister sourceRegister) {
                        Debug.Assert(offset == 0);
                        instruction.WriteLine("\t" + operation + " " + AsmName + "," + sourceRegister);
                    }
                    else {
                        instruction.WriteLine("\t" + operation + " " + AsmName + ",(" + variable.MemoryAddress(offset) + ")");
                    }
                    break;
                }
            case IndirectOperand indirectOperand: {
                    var pointer = indirectOperand.Variable;
                    var offset = indirectOperand.Offset;
                    {
                        var register = instruction.GetVariableRegister(pointer, 0);
                        if (register is WordRegister pointerRegister) {
                            ViaPointerRegister(pointerRegister);
                            return;
                        }
                    }
                    using var reservation = WordOperation.ReserveAnyRegister(instruction, Candidates());
                    reservation.WordRegister.LoadFromMemory(instruction, pointer, 0);
                    ViaPointerRegister(reservation.WordRegister);
                    break;

                    void ViaPointerRegister(Cate.WordRegister pointerRegister)
                    {
                        var pointerName = Tlcs900.WordRegister.PointerName(pointerRegister);
                        if (offset == 0) {
                            instruction.WriteLine("\t" + operation + " " + AsmName + ",(" + pointerName + ")");
                        }
                        else {
                            instruction.WriteLine("\t" + operation + " " + AsmName + ",(" + pointerName + "+" + offset + ")");
                        }
                    }
                }
            default:
                throw new NotImplementedException();
        }
        if (change) instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
        return;

        List<Cate.WordRegister> Candidates()
        {
            return All.Where(r => !r.Conflicts(this)).Cast<Cate.WordRegister>().ToList();
        }
    }

    public override bool IsOffsetInRange(int offset)
    {
        return offset is >= -0x8000 and <= 0x7fff;
    }

    public override void Add(Instruction instruction, int offset)
    {
        switch (offset) {
            case 0:
                return;
            case >= 1 and <= 8:
                instruction.WriteLine("\tinc " + offset + "," + this);
                return;
            case <= -1 and >= -8:
                instruction.WriteLine("\tdec " + (-offset) + "," + this);
                return;
            case > 0:
                instruction.WriteLine("\tadd " + this + "," + offset);
                return;
            default:
                instruction.WriteLine("\tsub " + this + "," + (-offset));
                break;
        }
    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpush " + AsmName + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpop " + AsmName + comment);
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tpush " + AsmName);
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpop " + AsmName);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0) {
            instruction.WriteLine("\txor " + AsmName + "," + AsmName);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    protected override void LoadConstant(Instruction instruction, PointerOperand pointerOperand)
    {
        instruction.WriteLine("\tld " + this + "," + pointerOperand.MemoryAddress());
        instruction.RemoveRegisterAssignment(this);
        instruction.SetRegisterConstant(this, pointerOperand);
        instruction.AddChanged(this);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tld " + AsmName + "," + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tld " + AsmName + ",(" + label + ")");
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tld (" + label + ")," + AsmName);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        var pointerName = PointerName(pointerRegister);
        if (offset == 0) {
            instruction.WriteLine("\tld " + AsmName + ",(" + pointerName + ")");
        }
        else {
            instruction.WriteLine("\tld " + AsmName + ",(" + pointerName + "+" + offset + ")");
        }
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        var pointerName = PointerName(pointerRegister);
        if (offset == 0) {
            instruction.WriteLine("\tld (" + pointerName + ")," + AsmName);
        }
        else {
            instruction.WriteLine("\tld (" + pointerName + "+" + offset + ")," + AsmName);
        }
    }

    public static string PointerName(Cate.WordRegister pointerRegister)
    {
        return "x" + pointerRegister.AsmName;
    }
}