using System;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate;

public abstract class WordRegister : Register
{
    protected WordRegister(int id, string name) : base(id, 2, name) { }

    protected WordRegister(int id, int byteCount, string name) : base(id, byteCount, name) { }

    public virtual ByteRegister? Low => null;
    public virtual ByteRegister? High => null;

    public virtual bool IsPair() => Low != null && High != null;


    public virtual bool Contains(ByteRegister byteRegister)
    {
        return Equals(Low, byteRegister) || Equals(High, byteRegister);
    }
    public override bool Conflicts(Register? register)
    {
        return register switch
        {
            ByteRegister byteRegister => Contains(byteRegister),
            _ => base.Conflicts(register)
        };
    }

    public override bool Matches(Register register)
    {
        return base.Matches(register) || register is ByteRegister byteRegister && Contains(byteRegister);
    }

    public override void LoadConstant(Instruction instruction, int value)
    {
        if (instruction.IsConstantAssigned(this, value)) {
            instruction.AddChanged(this);
            return;
        }
        LoadConstant(instruction, value.ToString());
        instruction.SetRegisterConstant(this, value);
    }

    protected virtual void LoadConstant(Instruction instruction, PointerOperand pointerOperand)
    {
        LoadConstant(instruction, pointerOperand.MemoryAddress());
        instruction.SetRegisterConstant(this, pointerOperand);
        instruction.AddChanged(this);
    }

    public void Load(Instruction instruction, Operand sourceOperand)
    {
        switch (sourceOperand) {
            case NullPointerOperand:
                if (instruction.IsConstantAssigned(this, 0)) return;
                LoadConstant(instruction, 0);
                instruction.SetRegisterConstant(this, 0);
                instruction.AddChanged(this);
                return;
            case PointerOperand sourcePointerOperand:
                if (instruction.IsConstantAssigned(this, sourcePointerOperand)) return;
                LoadConstant(instruction, sourcePointerOperand);
                return;
            case IntegerOperand sourceIntegerOperand:
                var value = sourceIntegerOperand.IntegerValue;
                if (instruction.IsConstantAssigned(this, value)) return;
                LoadConstant(instruction, value);
                instruction.SetRegisterConstant(this, value);
                instruction.AddChanged(this);
                return;
            case VariableOperand sourceVariableOperand: {
                    var sourceVariable = sourceVariableOperand.Variable;
                    var sourceOffset = sourceVariableOperand.Offset;
                    var variableRegister = instruction.GetVariableRegister(sourceVariableOperand, r => r.Equals(this)) ??
                                           instruction.GetVariableRegister(sourceVariableOperand);
                    if (variableRegister is WordRegister sourceRegister) {
                        Debug.Assert(sourceOffset == 0);
                        if (!Equals(sourceRegister, this)) {
                            CopyFrom(instruction, sourceRegister);
                            instruction.CancelOperandRegister(sourceVariableOperand);
                        }
                        return;
                    }
                    LoadFromMemory(instruction, sourceVariable, sourceOffset);
                    instruction.CancelOperandRegister(sourceVariableOperand);
                    return;
                }
            case IndirectOperand sourceIndirectOperand: {
                    var pointer = sourceIndirectOperand.Variable;
                    var offset = sourceIndirectOperand.Offset;
                    var variableRegister = pointer.Register ?? instruction.GetVariableRegister(pointer, 0);
                    if (variableRegister is WordRegister pointerRegister) {
                        if (pointerRegister.IsOffsetInRange(0)) {
                            LoadIndirect(instruction, pointerRegister, offset);
                            instruction.AddChanged(this);
                            instruction.CancelOperandRegister(sourceIndirectOperand);
                            return;
                        }
                        var candidates = WordOperation.Registers.Where(r => !r.Equals(this) && r.IsOffsetInRange(offset)).ToList();
                        if (candidates.Any()) {
                            using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
                            reservation.WordRegister.CopyFrom(instruction, pointerRegister);
                            LoadIndirect(instruction, reservation.WordRegister, offset);
                            instruction.AddChanged(this);
                            instruction.CancelOperandRegister(sourceIndirectOperand);
                            return;
                        }
                    }
                    LoadIndirect(instruction, pointer, offset);
                    instruction.AddChanged(this);
                    instruction.CancelOperandRegister(sourceIndirectOperand);
                    return;
                }
        }
        throw new NotImplementedException();
    }

    public virtual void Store(Instruction instruction, AssignableOperand destinationOperand)
    {
        switch (destinationOperand) {
            case VariableOperand destinationVariableOperand: {
                    var destinationVariable = destinationVariableOperand.Variable;
                    var destinationOffset = destinationVariableOperand.Offset;
                    if (destinationVariable.Register is WordRegister destinationWordRegister) {
                        Debug.Assert(destinationOffset == 0);
                        if (!Equals(destinationWordRegister, this)) {
                            destinationWordRegister.CopyFrom(instruction, this);
                        }
                        instruction.SetVariableRegister(destinationVariable, destinationOffset, destinationWordRegister);
                        return;
                    }
                    StoreToMemory(instruction, destinationVariable, destinationOffset);
                    return;
                }
            case IndirectOperand destinationIndirectOperand: {
                    var destinationPointer = destinationIndirectOperand.Variable;
                    var destinationOffset = destinationIndirectOperand.Offset;
                    StoreIndirect(instruction, destinationPointer, destinationOffset);
                    return;
                }
        }
        throw new NotImplementedException();
    }


    public abstract void CopyFrom(Instruction instruction, WordRegister sourceRegister);


    public abstract void Operate(Instruction instruction, string operation, bool change, Operand operand);

    public virtual void TemporaryOffset(Instruction instruction, int offset, Action action)
    {
        if (instruction.IsRegisterReserved(this)) {
            var changed = instruction.IsChanged(this);
            Add(instruction, offset);
            action();
            if (changed) return;
            Add(instruction, -offset);
            instruction.RemoveChanged(this);
        }
        else {
            var changed = instruction.IsChanged(this);
            using var reservation = instruction.ReserveRegister(this);
            Add(instruction, offset);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            action();
            if (!changed) {
                Add(instruction, -offset);
                instruction.RemoveChanged(this);
            }
        }
    }

    public virtual void Exchange(Instruction instruction, WordRegister register)
    {
        Debug.Assert(!Equals(this, register));
        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordOperation.Registers.Where(r => !Equals(r, this) && !Equals(r, register)).ToList());
        reservation.WordRegister.CopyFrom(instruction, register);
        register.CopyFrom(instruction, this);
        CopyFrom(instruction, reservation.WordRegister);
    }

    public abstract bool IsOffsetInRange(int offset);
    public abstract void Add(Instruction instruction, int offset);

    public virtual void Compare(Instruction instruction, string operation, Operand operand)
    {
        Operate(instruction, operation, false, operand);
    }
}