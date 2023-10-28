using System;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mc6800.Mc6801;

internal class PairRegister : Cate.WordRegister
{
    public static PairRegister D = new(4, "d", ByteRegister.A, ByteRegister.B);

    private readonly ByteRegister high;
    private readonly ByteRegister low;

    public PairRegister(int id, string name, ByteRegister high, ByteRegister low) : base(id, name)
    {
        this.high = high;
        this.low = low;
    }

    public override Cate.ByteRegister? Low => low;
    public override Cate.ByteRegister? High => high;

    public override bool Conflicts(Register? register)
    {
        if (register is ByteRegister byteRegister && Contains(byteRegister)) {
            return true;
        }
        return base.Conflicts(register);
    }

    public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
    {
        low.Save(writer, comment, jump, tabCount);
        high.Save(writer, comment, jump, tabCount);
    }

    public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
    {
        high.Restore(writer, comment, jump, tabCount);
        low.Restore(writer, comment, jump, tabCount);
    }

    public override void Save(Instruction instruction)
    {
        low.Save(instruction);
        high.Save(instruction);
    }

    public override void Restore(Instruction instruction)
    {
        high.Restore(instruction);
        low.Restore(instruction);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0) {
            instruction.WriteLine("\tclra");
            instruction.WriteLine("\tclrb");
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tld" + AsmName + "\t#" + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {

        instruction.WriteLine("\tld" + AsmName + "\t" + label);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {

        instruction.RemoveRegisterAssignment(this);
        instruction.WriteLine("\tst" + AsmName + "\t" + label);
    }

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        Debug.Assert(Equals(this, D));
        Debug.Assert(Equals(pointerRegister, PointerRegister.X));
        while (true) {
            if (PointerRegister.X.IsOffsetInRange(offset)) {
                instruction.WriteLine("\tld" + AsmName + "\t" + offset + ",x");
                instruction.ResultFlags |= Instruction.Flag.Z;
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            pointerRegister.Add(instruction, offset);
            offset = 0;
        }
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        Debug.Assert(Equals(this, D));
        Debug.Assert(Equals(pointerRegister, PointerRegister.X));
        while (true) {
            if (PointerRegister.X.IsOffsetInRange(offset)) {
                instruction.WriteLine("\tst" + AsmName + "\t" + offset + ",x");
                return;
            }
            pointerRegister.Add(instruction, offset);
            offset = 0;
        }
    }


    public override void CopyFrom(Instruction instruction, WordRegister sourceRegister)
    {
        if (Equals(sourceRegister, this)) return;
        var label = ZeroPage.Word.Label;
        sourceRegister.StoreToMemory(instruction, label);
        LoadFromMemory(instruction, label);
        instruction.SetRegisterCopy(this, sourceRegister);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        switch (operand) {
            case IntegerOperand integerOperand:
                Debug.Assert(!change);
                OperateConstant(instruction, operation, change, integerOperand.IntegerValue);
                return;
            case PointerOperand pointerOperand:
                Debug.Assert(!change);
                OperateConstant(instruction, operation, change, pointerOperand.MemoryAddress());
                return;
            case StringOperand stringOperand:
                Debug.Assert(!change);
                OperateConstant(instruction, operation, change, stringOperand.StringValue);
                return;
            case VariableOperand variableOperand:
                var variable = variableOperand.Variable;
                var offset = variableOperand.Offset;
                OperateMemory(instruction, operation, change, variable, offset);
                return;
        }
        throw new NotImplementedException();
    }

    private void OperateConstant(Instruction instruction, string operation, bool change, int value)
    {
        if (value == 0) {
            high.LoadConstant(instruction, 0);
            low.LoadConstant(instruction, 0);
        }
        else {
            OperateConstant(instruction, operation, change, value.ToString());
        }
    }

    private void OperateConstant(Instruction instruction, string operation, bool change, string value)
    {
        instruction.WriteLine("\t" + operation + Name + "\t#" + value);
        instruction.ResultFlags |= Instruction.Flag.Z;
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    private void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset)
    {
        var variableRegister = variable.Register;
        if (variableRegister is IndexRegister rightRegister) {
            Debug.Assert(operation.Replace("\t", "").Length == 3);
            rightRegister.StoreToMemory(instruction, ZeroPage.Word.Name);
            OperateMemory(instruction, operation, change, ZeroPage.Word.Name);
            return;
        }
        OperateMemory(instruction, operation, change, variable.MemoryAddress(offset));
    }

    private void OperateMemory(Instruction instruction, string operation, bool change, string label)
    {
        instruction.WriteLine("\t" + operation + Name + "\t" + label);
        if (!change)
            return;
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }
}