using Microsoft.Win32;

namespace Inu.Cate.Wdc65816;

internal abstract class WordRegister(ByteRegister byteRegister) : Cate.WordRegister(byteRegister.Id + IdOffset, byteRegister.Name)
{
    public static readonly WordAccumulator A = new(ByteRegister.A);
    public static readonly WordIndexRegister X = new(ByteRegister.X);
    public static readonly WordIndexRegister Y = new(ByteRegister.Y);
    public static readonly List<Cate.WordRegister> Registers = [A, X, Y];

    private const int IdOffset = 4;
    public readonly ByteRegister ByteRegister = byteRegister;

    public static List<Cate.WordRegister> PointerRegisters => ((List<Cate.WordRegister>)[X, Y]).Union(WordZeroPage.Registers).ToList();

    public abstract void MakeSize(Instruction instruction);

    public override bool Contains(Cate.ByteRegister byteRegister)
    {
        return ByteRegister.Equals(byteRegister);
    }

    public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
    {
        Compiler.CopyFrom(this, instruction, sourceRegister);
    }

    public override bool IsOffsetInRange(int offset) => true;

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Compiler.Save(this, writer, comment, instruction, tabCount);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Compiler.Restore(this, writer, comment, instruction, tabCount);
    }

    public override void Save(Instruction instruction)
    {
        Compiler.Save(this, instruction);
    }

    public override void Restore(Instruction instruction)
    {
        Compiler.Restore(this, instruction);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        Compiler.LoadConstant(this, instruction, value);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        Compiler.LoadFromMemory(this, instruction, label);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        Compiler.StoreToMemory(this, instruction, label);
    }

    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        Compiler.LoadFromMemory(this, instruction, variable, offset);
    }

    public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
    {
        Compiler.StoreToMemory(this, instruction, variable, offset);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        Compiler.LoadIndirect(this, instruction, pointerRegister, offset);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        Compiler.StoreIndirect(this, instruction, pointerRegister, offset);
    }
}

internal class WordAccumulator(ByteAccumulator byteAccumulator) : WordRegister(byteAccumulator)
{
    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (operand is VariableOperand variableOperand) {
            var variableRegister = instruction.GetVariableRegister(variableOperand);
            switch (variableRegister) {
                case WordZeroPage zeroPage:
                    MakeSize(instruction);
                    instruction.WriteLine("\t" + operation + "\t" + zeroPage);
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    if (!change)
                        return;
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
                case WordRegister wordRegister:
                    using (var reservation = WordOperation.ReserveAnyRegister(instruction, WordZeroPage.Registers)) {
                        var temporary = reservation.WordRegister;
                        temporary.CopyFrom(instruction, wordRegister);
                        MakeSize(instruction);
                        instruction.WriteLine("\t" + operation + "\t" + temporary);
                    }
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    if (!change)
                        return;
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
            }
            MakeSize(instruction);
            instruction.WriteLine("\t" + operation + "\t" + variableOperand.MemoryAddress());
            instruction.ResultFlags |= Instruction.Flag.Z;
            if (!change)
                return;
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }
        using (var reservation = WordOperation.ReserveAnyRegister(instruction, WordZeroPage.Registers)) {
            var temporary = reservation.WordRegister;
            temporary.Load(instruction, operand);
            MakeSize(instruction);
            instruction.WriteLine("\t" + operation + "\t" + temporary);
        }
    }

    public override void Add(Instruction instruction, int offset)
    {
        MakeSize(instruction);
        instruction.WriteLine("\tclc|adc\t#" + offset);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void MakeSize(Instruction instruction)
    {
        ModeFlag.Memory.ResetBit(instruction);
    }

    public void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        MakeSize(instruction);
        instruction.WriteLine("\t" + operation + "\t" + operand);
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }
}

internal class WordIndexRegister(ByteIndexRegister byteIndexRegister) : WordRegister(byteIndexRegister)
{
    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        using (WordOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.Operate(instruction, operation, change, operand);
            if (change) {
                CopyFrom(instruction, A);
            }
        }
    }

    public override void Add(Instruction instruction, int offset)
    {
        using (WordOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.Add(instruction, offset);
        }
    }

    public override void Compare(Instruction instruction, string operation, Operand operand)
    {
        switch (operand) {
            case IntegerOperand integerOperand:
                MakeSize(instruction);
                instruction.WriteLine("\tcp" + AsmName + "\t#" + integerOperand.IntegerValue);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            case VariableOperand variableOperand:
                if (variableOperand.Register == null) {
                    MakeSize(instruction);
                    instruction.WriteLine("\tcp" + AsmName + "\t" + variableOperand.MemoryAddress());
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    return;
                }
                if (variableOperand.Register is WordZeroPage wordZeroPage) {
                    MakeSize(instruction);
                    instruction.WriteLine("\tcp" + AsmName + "\t" + wordZeroPage.AsmName);
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    return;
                }
                if (variableOperand.Register is WordRegister operandRegister)
                {
                    using var reservation = WordOperation.ReserveAnyRegister(instruction, WordZeroPage.Registers);
                    reservation.WordRegister.CopyFrom(instruction, operandRegister);
                    MakeSize(instruction);
                    instruction.WriteLine("\tcp" + AsmName + "\t" + reservation.WordRegister.AsmName);
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    return;
                }
                break;
        }
        base.Compare(instruction, operation, operand);
    }

    public override void MakeSize(Instruction instruction)
    {
        ModeFlag.IndexRegister.ResetBit(instruction);
    }
}