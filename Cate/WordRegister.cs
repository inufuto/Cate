using System;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate;

public abstract class WordRegister : Register
{
    protected WordRegister(int id, string name) : base(id, 2, name) { }
    public virtual ByteRegister? Low => null;
    public virtual ByteRegister? High => null;

    public virtual bool IsPair() => Low != null && High != null;

    public PointerRegister? ToPointer()
    {
        return PointerOperation.Registers.FirstOrDefault(p => Equals(p.WordRegister, this));
    }


    public virtual bool Contains(ByteRegister byteRegister)
    {
        return Equals(Low, byteRegister) || Equals(High, byteRegister);
    }
    public override bool Conflicts(Register? register)
    {
        return register switch
        {
            WordPointerRegister wordPointerRegister => Conflicts(wordPointerRegister.WordRegister),
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
                LoadConstant(instruction, sourcePointerOperand.MemoryAddress());
                instruction.SetRegisterConstant(this, sourcePointerOperand);
                instruction.AddChanged(this);
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
                if (variableRegister is PointerRegister pointerRegister)
                    variableRegister = pointerRegister.WordRegister;
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
                if (variableRegister is WordRegister wRegister) {
                    variableRegister = wRegister.ToPointer();
                }
                if (variableRegister is PointerRegister pointerRegister) {
                    if (pointerRegister.IsOffsetInRange(0)) {
                        LoadIndirect(instruction, pointerRegister, offset);
                        instruction.AddChanged(this);
                        instruction.CancelOperandRegister(sourceIndirectOperand);
                        return;
                    }
                    var candidates = PointerOperation.Registers.Where(r => r.IsOffsetInRange(offset)).ToList();
                    if (candidates.Any()) {
                        var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
                        reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
                        LoadIndirect(instruction, reservation.PointerRegister, offset);
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
                if (destinationVariable.Register is WordPointerRegister destinationPointerRegister) {
                    Debug.Assert(destinationOffset == 0);
                    var wordRegister = destinationPointerRegister.WordRegister;
                    if (!Equals(wordRegister, this)) {
                        wordRegister?.CopyFrom(instruction, this);
                    }
                    instruction.SetVariableRegister(destinationVariable, destinationOffset, wordRegister);
                    return;
                }
                StoreToMemory(instruction, destinationVariable, destinationOffset);
                return;
            }
            case IndirectOperand destinationIndirectOperand: {
                var destinationPointer = destinationIndirectOperand.Variable;
                var destinationOffset = destinationIndirectOperand.Offset;
                if (destinationPointer.Register is PointerRegister destinationPointerRegister) {
                    StoreIndirect(instruction,
                        destinationPointerRegister, destinationOffset);
                    return;
                }
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerOperation.RegistersToOffset(destinationOffset));
                reservation.PointerRegister.LoadFromMemory(instruction, destinationPointer,0);
                StoreIndirect(instruction, reservation.PointerRegister, destinationOffset);
                return;
            }
        }
        throw new NotImplementedException();
    }


    public abstract void CopyFrom(Instruction instruction, WordRegister sourceRegister);


    public abstract void Operate(Instruction instruction, string operation, bool change, Operand operand);

    public virtual void Exchange(Instruction instruction, WordRegister register)
    {
        Debug.Assert(!Equals(this, register));
        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordOperation.Registers.Where(r => !Equals(r, this) && !Equals(r, register)).ToList());
        reservation.WordRegister.CopyFrom(instruction, register);
        register.CopyFrom(instruction, this);
        CopyFrom(instruction, reservation.WordRegister);
    }
}