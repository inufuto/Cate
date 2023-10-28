using System;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mc6800;

internal class IndexRegister : Cate.WordRegister
{
    public static IndexRegister X = new(3, "x");

    private protected IndexRegister(int id, string name) : base(id, name) { }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tldx\t#" + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(X);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        Debug.Assert(Equals(X));
        instruction.WriteLine("\tldx\t" + label);
    }


    public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
    {
        Debug.Assert(Equals(this, X));
        if (Equals(instruction.GetVariableRegister(variable, offset), this))
            return;
        LoadFromMemory(instruction, variable.MemoryAddress(offset));
        instruction.AddChanged(this);
        instruction.SetVariableRegister(variable, offset, this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        Debug.Assert(Equals(X));
        instruction.RemoveRegisterAssignment(this);
        instruction.WriteLine("\tstx\t" + label);
    }


    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        Debug.Assert(Equals(this, IndexRegister.X));
        Debug.Assert(Equals(pointerRegister, PointerRegister.X));
        while (true) {
            if (PointerRegister.X.IsOffsetInRange(offset)) {
                instruction.WriteLine("\tldx\t" + offset + ",x");
                instruction.ResultFlags |= Instruction.Flag.Z;
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            pointerRegister.Add(instruction, offset);
            offset = 0;
        }
    }

    public override void LoadIndirect(Instruction instruction, Variable pointer, int offset)
    {
        PointerRegister.X.LoadFromMemory(instruction, pointer, 0);
        LoadIndirect(instruction, PointerRegister.X, offset);
    }

    public override void StoreIndirect(Instruction instruction, Variable pointer, int offset)
    {
        StoreToMemory(instruction, "<"+ZeroPage.Word.Label);
        var variableRegister = instruction.GetVariableRegister(pointer, 0);
        if (Equals(variableRegister, PointerRegister.X)) {
            ViaRegister();
            return;
        }
        //using var pointerReservation = PointerOperation.ReserveRegister(instruction, PointerRegister.X);
        ViaRegister();
        return;

        void ViaRegister()
        {
            X.LoadFromMemory(instruction, pointer, 0);
            using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
            var byteRegister = reservation.ByteRegister;
            byteRegister.LoadFromMemory(instruction, ZeroPage.Word.High.Label);
            instruction.WriteLine("\tsta" + byteRegister.AsmName + "\t" + offset + "+0,x");
            byteRegister.LoadFromMemory(instruction, ZeroPage.Word.Low.Label);
            instruction.WriteLine("\tsta" + byteRegister.AsmName + "\t" + offset + "+1,x");
        }
    }


    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        throw new NotImplementedException();
    }

    //public override void Store(Instruction instruction, AssignableOperand destinationOperand)
    //{
    //    Debug.Assert(Equals(this, X));

    //    switch (destinationOperand) {
    //        case VariableOperand variableOperand:
    //            StoreToMemory(instruction, variableOperand.MemoryAddress());
    //            instruction.SetVariableRegister(variableOperand, this);
    //            return;
    //        case IndirectOperand indirectOperand:
    //            var pointer = indirectOperand.Variable;
    //            var offset = indirectOperand.Offset;
    //            instruction.WriteLine("\tstx\t" + ZeroPage.Word);
    //            Debug.Assert(pointer.Register == null);
    //            var pointerRegister = PointerRegister.X;
    //            pointerRegister.LoadFromMemory(instruction, pointer, 0);
    //            using (var reservation = ByteOperation.ReserveAnyRegister(instruction)) {
    //                var register = reservation.ByteRegister;
    //                instruction.WriteLine("\tlda" + register + "\t" + ZeroPage.WordLow);
    //                register.StoreIndirect(instruction, pointerRegister, offset + 1);
    //                instruction.WriteLine("\tlda" + register + "\t" + ZeroPage.WordHigh);
    //                register.StoreIndirect(instruction, pointerRegister, offset);
    //            }
    //            return;
    //    }
    //    throw new NotImplementedException();
    //}

    public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
    {
        if (Equals(sourceRegister, this)) return;
        // Cannot copy because word register is only one
        throw new NotImplementedException();
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

    private void OperateConstant(Instruction instruction, string operation, bool change, int value)
    {
        OperateConstant(instruction, operation, change, value.ToString());
    }

    private void OperateMemory(Instruction instruction, string operation, bool change, string label)
    {
        instruction.WriteLine("\t" + operation + Name + "\t" + label);
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

    public override void Save(Instruction instruction)
    {
        StoreToMemory(instruction, ZeroPage.Word.Name);
        using var reservation = ByteOperation.ReserveAnyRegister(instruction);
        var register = reservation.ByteRegister;
        register.LoadFromMemory(instruction, ZeroPage.Word.High.Name);
        instruction.WriteLine("\tpsh" + register);
        register.LoadFromMemory(instruction, ZeroPage.Word.Low.Name);
        instruction.WriteLine("\tpsh" + register);
    }

    public override void Restore(Instruction instruction)
    {
        using (var reservation = ByteOperation.ReserveAnyRegister(instruction)) {
            var register = reservation.ByteRegister;
            instruction.WriteLine("\tpul" + register);
            register.LoadFromMemory(instruction, ZeroPage.Word.Low.Name);
            instruction.WriteLine("\tpul" + register);
            register.LoadFromMemory(instruction, ZeroPage.Word.High.Name);
        }
        LoadFromMemory(instruction, ZeroPage.Word.Name);
    }


    public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
    {
        // cannot save : don't assign to variable
        throw new NotImplementedException();
    }

    public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
    {
        // cannot save : don't assign to variable
        throw new NotImplementedException();
    }
}