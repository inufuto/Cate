using System.Diagnostics;
using System.Xml.Linq;

namespace Inu.Cate.Tlcs900;

internal class ByteRegister(int id, string name) : Cate.ByteRegister(id, name)
{
    public static readonly ByteRegister A = new(1, "a");
    public static readonly ByteRegister W = new(2, "w");
    public static readonly ByteRegister B = new(3, "b");
    public static readonly ByteRegister C = new(4, "c");
    public static readonly ByteRegister D = new(5, "d");
    public static readonly ByteRegister E = new(6, "e");
    public static readonly ByteRegister H = new(7, "h");
    public static readonly ByteRegister L = new(8, "l");

    public static readonly ByteRegister IXL = new(9, "ixl");
    public static readonly ByteRegister IXH = new(10, "ixh");
    public static readonly ByteRegister IYL = new(11, "iyl");
    public static readonly ByteRegister IYH = new(12, "iyh");
    public static readonly ByteRegister IZL = new(13, "izl");
    public static readonly ByteRegister IZH = new(14, "izh");

    public static List<ByteRegister> All = [A, W, B, C, D, E, H, L, IXL, IXH, IYL, IYH, IZL, IZH];
    public static List<ByteRegister> Standard = [A, W, B, C, D, E, H, L];

    public WordRegister? WordRegister => WordRegister.All.FirstOrDefault(r => r.Contains(this));
    public override Cate.WordRegister? PairRegister => WordRegister;


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
        if (value == 0 && Standard.Contains(this)) {
            instruction.WriteLine("\txor " + AsmName + "," + AsmName);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tld " + AsmName + "," + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        if (Standard.Contains(this)) {
            instruction.WriteLine("\tld " + AsmName + ",(" + label + ")");
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }
        else {
            using var reservation = ByteOperation.ReserveAnyRegister(instruction, Standard.Cast<Cate.ByteRegister>().ToList());
            reservation.ByteRegister.LoadFromMemory(instruction, label);
            CopyFrom(instruction, reservation.ByteRegister);
        }
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        if (Standard.Contains(this)) {
            instruction.WriteLine("\tld (" + label + ")," + AsmName);
        }
        else {
            using var reservation = ByteOperation.ReserveAnyRegister(instruction, Standard.Cast<Cate.ByteRegister>().ToList());
            reservation.ByteRegister.CopyFrom(instruction, this);
            reservation.ByteRegister.StoreToMemory(instruction, label);
        }
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        var pointerName = Tlcs900.WordRegister.PointerName(pointerRegister);
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
        var pointerName = Tlcs900.WordRegister.PointerName(pointerRegister);
        if (offset == 0) {
            instruction.WriteLine("\tld (" + pointerName + ")," + AsmName);
        }
        else {
            instruction.WriteLine("\tld (" + pointerName + "+" + offset + ")," + AsmName);
        }
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
    {
        if (Equals(sourceRegister, this)) return;

        instruction.WriteLine("\tld " + this + "," + sourceRegister);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        throw new NotImplementedException();
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        switch (operand) {
            case IntegerOperand integerOperand: {
                    instruction.WriteLine("\t" + operation + " " + AsmName + "," + integerOperand.IntegerValue);
                    break;
                }
            case VariableOperand variableOperand: {
                    var variable = variableOperand.Variable;
                    var offset = variableOperand.Offset;
                    var register = instruction.GetVariableRegister(variableOperand);
                    if (register is ByteRegister sourceRegister) {
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
            return WordRegister.All.Where(r => !r.Conflicts(this)).Cast<Cate.WordRegister>().ToList();
        }
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        throw new NotImplementedException();
    }
}