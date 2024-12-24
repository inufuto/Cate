namespace Inu.Cate.Wdc65816;

internal abstract class ByteRegister(int id, string name) : Cate.ByteRegister(id, name)
{
    public static readonly ByteAccumulator A = new(1, "a");

    public static readonly List<Cate.ByteRegister> Registers = [A];

    public abstract void MakeSize(Instruction instruction);

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
        MakeSize(instruction);
        instruction.WriteLine("\tst" + Name + "\t" + variable.MemoryAddress(offset));
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        Compiler.LoadIndirect(this, instruction, pointerRegister, offset);
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        Compiler.StoreIndirect(this, instruction, pointerRegister, offset);
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
    {
        Compiler.CopyFrom(this, instruction, sourceRegister);
    }

    public abstract void Decrement(Instruction instruction);
}

internal class ByteAccumulator(int id, string name) : ByteRegister(id, name)
{
    public Cate.WordRegister? WordRegister => Wdc65816.WordRegister.Registers.Find(r => r is WordAccumulator a && a.ByteRegister.Equals(this));

    public override void MakeSize(Instruction instruction)
    {
        ModeFlag.Memory.SetBit(instruction);
    }

    public override void Decrement(Instruction instruction)
    {
        MakeSize(instruction);
        instruction.WriteLine("\tdec\ta");
    }

    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        MakeSize(instruction);
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + "\t" + Name);
        }
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (operand is VariableOperand variableOperand) {
            var variableRegister = instruction.GetVariableRegister(variableOperand);
            switch (variableRegister) {
                case ByteZeroPage zeroPage:
                    MakeSize(instruction);
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
        }
        ByteOperation.Operate(instruction, operation, change, operand);
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        MakeSize(instruction);
        instruction.WriteLine("\t" + operation + "\t" + operand);
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }
}
