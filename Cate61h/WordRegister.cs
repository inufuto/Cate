using Microsoft.Win32;

namespace Inu.Cate.Hd61700;

internal class WordRegister : Cate.WordRegister
{
    public const int Count = 10;
    public static readonly List<Cate.WordRegister> Registers = [];

    static WordRegister()
    {
        for (var i = 0; i < Count; ++i) {
            Registers.Add(new WordRegister(ByteRegister.Count + i * 2));
        }
    }

    private WordRegister(int id) : base(id, Compiler.RegisterHead + id) { }
    public string HighByteName => Compiler.RegisterHead + (Id + 1);


    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tphsw " + HighByteName + (comment != "" ? " ;" + comment : ""));
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tppsw " + AsmName + (comment != "" ? " ;" + comment : ""));
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tphsw " + HighByteName);
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tppsw " + AsmName);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        switch (value) {
            case 0:
                instruction.WriteLine("\txrw " + AsmName + "," + AsmName);
                instruction.SetRegisterConstant(this, value);
                break;
            case 1:
                instruction.WriteLine("\tldw " + AsmName + ",$sy");
                instruction.SetRegisterConstant(this, value);
                break;
            default:
                base.LoadConstant(instruction, value);
                break;
        }
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tldw " + AsmName + "," + value);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(true));
        var pointerRegister = reservation.WordRegister;
        pointerRegister.LoadConstant(instruction, label);
        LoadIndirect(instruction, pointerRegister, 0);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(true));
        var pointerRegister = reservation.WordRegister;
        pointerRegister.LoadConstant(instruction, label);
        StoreIndirect(instruction, pointerRegister, 0);
    }

    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        using (var reservation = WordOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(true))) {
            var indexRegister = (IndexRegister)reservation.WordRegister;
            indexRegister.LoadConstant(instruction, variable, offset);
            instruction.SetRegisterConstant(indexRegister, new PointerType(variable.Type), variable, offset);
            LoadIndirect(instruction, indexRegister, 0);
        }
        instruction.SetVariableRegister(variable, offset, this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
    {
        using (var reservation = WordOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(true))) {
            var indexRegister = (IndexRegister)reservation.WordRegister;
            indexRegister.LoadConstant(instruction, variable, offset);
            StoreIndirect(instruction, indexRegister, 0);
        }

        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        if (pointerRegister is IndexRegister indexRegister) {
            var sign = offset >= 0 ? "+" : "-";
            var abs = Math.Abs(offset);
            switch (abs) {
                case 0:
                    WithOffset("$sx");
                    break;
                case 1:
                    WithOffset("$sy");
                    break;
                default: {
                        using var reservation = ByteOperation.ReserveAnyRegister(instruction);
                        var byteRegister = reservation.ByteRegister;
                        byteRegister.LoadConstant(instruction, abs);
                        WithOffset(byteRegister.AsmName);
                        break;
                    }
            }

            void WithOffset(string offsetValue)
            {
                instruction.WriteLine("\tldw " + AsmName + ",(" + indexRegister.AsmName + sign + offsetValue + ")");
            }
        }
        else {
            if (offset == 0) {
                instruction.WriteLine("\tldw " + AsmName + ",(" + pointerRegister.AsmName + ")");
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(false));
                reservation.WordRegister.CopyFrom(instruction, pointerRegister);
                LoadIndirect(instruction, reservation.WordRegister, offset);
            }
        }
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        if (pointerRegister is IndexRegister indexRegister) {
            if (offset == 0) {
                instruction.WriteLine("\tstw " + AsmName + ",(" + indexRegister.AsmName + "+$sx)");
            }
            else if (offset == 1) {
                instruction.WriteLine("\tstw " + AsmName + ",(" + indexRegister.AsmName + "+$sy)");
            }
            else {
                using var reservation = ByteOperation.ReserveAnyRegister(instruction);
                var offsetRegister = reservation.ByteRegister;
                if (offset > 0) {
                    offsetRegister.LoadConstant(instruction, offset);
                    instruction.WriteLine("\tstw " + AsmName + ",(" + pointerRegister.AsmName + "+" + offsetRegister.AsmName + ")");
                }
                else {
                    offsetRegister.LoadConstant(instruction, -offset);
                    instruction.WriteLine("\tstw " + AsmName + ",(" + pointerRegister.AsmName + "-" + offsetRegister.AsmName + ")");
                }
            }
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(false));
            reservation.WordRegister.CopyFrom(instruction, pointerRegister);
            StoreIndirect(instruction, reservation.WordRegister, offset);
        }
    }

    public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
    {
        if (instruction.IsRegisterCopy(this, sourceRegister)) return;
        instruction.WriteLine("\tldw " + AsmName + "," + sourceRegister.AsmName);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
        instruction.SetRegisterCopy(this, sourceRegister);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (operand is IntegerOperand { IntegerValue: 1 }) {
            instruction.WriteLine("\t" + operation + " " + AsmName + ",$sy");
        }
        else {
            var candidates = Registers.Where(r => !r.Equals(this)).ToList();
            using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates, operand);
            var rightRegister = reservation.WordRegister;
            rightRegister.Load(instruction, operand);
            instruction.WriteLine("\t" + operation + " " + AsmName + "," + rightRegister.AsmName);
        }
        if (change) {
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }
    }

    public override bool IsOffsetInRange(int offset)
    {
        return Compiler.IsOffsetInRange(offset);
    }

    public override void Add(Instruction instruction, int offset)
    {
        using var reservation = WordOperation.ReserveAnyRegister(instruction);
        var wordRegister = reservation.WordRegister;
        if (offset > 0) {
            wordRegister.LoadConstant(instruction, offset);
            instruction.WriteLine("\tadw " + AsmName + "," + wordRegister.AsmName);
        }
        else {
            wordRegister.LoadConstant(instruction, -offset);
            instruction.WriteLine("\tsbw " + AsmName + "," + wordRegister.AsmName);
        }
    }

    public static Register? FromIndex(int index)
    {
        return index < Registers.Count ? Registers[index] : null;
    }
}