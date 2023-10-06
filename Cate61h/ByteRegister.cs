namespace Inu.Cate.Hd61700;

internal class ByteRegister : Cate.ByteRegister
{
    public const int Count = 10;
    public static readonly List<Cate.ByteRegister> Registers = new();
    static ByteRegister()
    {
        for (var i = 0; i < Count; ++i) {
            Registers.Add(new ByteRegister(i));
        }
    }


    private ByteRegister(int id) : base(id, Compiler.RegisterHead + id) { }

    public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tphs " + AsmName + (comment != "" ? " ;" + comment : ""));
    }

    public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpps " + AsmName + (comment != "" ? " ;" + comment : ""));
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tphs " + AsmName);
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpps " + AsmName);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        LoadConstant(instruction, IntValue(value));
        instruction.SetRegisterConstant(this, value);
        instruction.AddChanged(this);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tld " + AsmName + "," + value);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        using var reservation = PointerOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(true));
        var pointerRegister = reservation.PointerRegister;
        pointerRegister.LoadConstant(instruction, label);
        LoadIndirect(instruction, pointerRegister, 0);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        using var reservation = PointerOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(true));
        var pointerRegister = reservation.PointerRegister;
        pointerRegister.LoadConstant(instruction, label);
        StoreIndirect(instruction, pointerRegister, 0);
    }

    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        using (var reservation = PointerOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(true))) {
            var indexRegister = (IndexRegister)reservation.PointerRegister;
            indexRegister.LoadConstant(instruction, variable, offset);
            instruction.SetRegisterConstant(indexRegister, new PointerType(variable.Type), variable, offset);
            LoadIndirect(instruction, indexRegister, 0);
        }
        instruction.SetVariableRegister(variable, offset, this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
    {
        using (var reservation = PointerOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(true))) {
            var indexRegister = (IndexRegister)reservation.PointerRegister;
            indexRegister.LoadConstant(instruction, variable, offset);
            StoreIndirect(instruction, indexRegister, 0);
        }

        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void LoadIndirect(Instruction instruction, PointerRegister pointerRegister, int offset)
    {
        if (pointerRegister is IndexRegister && pointerRegister.IsOffsetInRange(offset)) {
            instruction.WriteLine("\tld " + AsmName + ",(" + pointerRegister.AsmName + IndexRegister.OffsetValue(offset) + ")");
        }
        else {
            if (offset == 0) {
                instruction.WriteLine("\tld " + AsmName + ",(" + pointerRegister.AsmName + ")");
                instruction.RemoveRegisterAssignment(this);
                instruction.AddChanged(this);
            }
            else if (Compiler.IsOffsetInRange(offset)) {
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(false));
                var indexRegister = reservation.PointerRegister;
                indexRegister.CopyFrom(instruction, pointerRegister);
                LoadIndirect(instruction, indexRegister, offset);
            }
            else {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    LoadIndirect(instruction, pointerRegister, 0);
                });
            }
        }
    }


    public override void StoreIndirect(Instruction instruction, PointerRegister pointerRegister, int offset)
    {
        if (pointerRegister is IndexRegister && pointerRegister.IsOffsetInRange(offset)) {
            instruction.WriteLine("\tst " + AsmName + ",(" + pointerRegister.AsmName + IndexRegister.OffsetValue(offset) + ")");
        }
        else {
            if (offset == 0) {
                instruction.WriteLine("\tst " + AsmName + ",(" + pointerRegister.AsmName + ")");
            }
            else if (Compiler.IsOffsetInRange(offset)) {
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, IndexRegister.Registers(false));
                var indexRegister = reservation.PointerRegister;
                indexRegister.CopyFrom(instruction, pointerRegister);
                StoreIndirect(instruction, indexRegister, offset);
            }
            else {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    StoreIndirect(instruction, pointerRegister, 0);
                });
            }
        }
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
    {
        if (instruction.IsRegisterCopy(this, sourceRegister)) return;
        instruction.WriteLine("\tld " + AsmName + "," + sourceRegister.AsmName);
        instruction.AddChanged(this);
        instruction.SetRegisterCopy(this, sourceRegister);
    }

    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + " " + AsmName);
        }
        if (change) {
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        switch (operand) {
            case IntegerOperand integerOperand:
                var value = integerOperand.IntegerValue;
                instruction.WriteLine("\t" + operation + " " + AsmName + "," + IntValue(value));
                instruction.RemoveRegisterAssignment(this);
                instruction.AddChanged(this);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            case VariableOperand variableOperand: {
                    var variableRegister = instruction.GetVariableRegister(variableOperand);
                    if (variableRegister is ByteRegister) {
                        instruction.WriteLine("\t" + operation + " " + AsmName + "," + variableRegister.AsmName);
                        instruction.RemoveRegisterAssignment(this);
                        instruction.AddChanged(this);
                        instruction.ResultFlags |= Instruction.Flag.Z;
                        return;
                    }
                    break;
                }
        }
        var candidates = Registers.Where(r => !r.Equals(this)).ToList();
        using var reservation = ByteOperation.ReserveAnyRegister(instruction, candidates, operand);
        var operandRegister = reservation.ByteRegister;
        operandRegister.Load(instruction, operand);
        instruction.WriteLine("\t" + operation + " " + AsmName + "," + operandRegister.AsmName);
        instruction.ResultFlags |= Instruction.Flag.Z;
        if (change) {
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }
    }

    public static string IntValue(int value)
    {
        return value switch
        {
            0 => "$sx",
            1 => "$sy",
            _ => value.ToString()
        };
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        throw new NotImplementedException();
    }

    public static Register? FromIndex(int index)
    {
        return index < Registers.Count ? Registers[index] : null;
    }
}