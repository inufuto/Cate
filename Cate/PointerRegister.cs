using System;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class PointerRegister : Register
    {
        protected PointerRegister(int id,int byteCount, string name) : base(id, byteCount, name) { }
        public abstract WordRegister? WordRegister { get; }
        //public abstract bool IsAddable();

        public abstract bool IsOffsetInRange(int offset);

        public virtual bool Contains(ByteRegister byteRegister)
        {
            return false;
        }

        public void Load(Instruction instruction, Operand sourceOperand)
        {
            switch (sourceOperand) {
                case IntegerOperand sourceIntegerOperand:
                    var value = sourceIntegerOperand.IntegerValue;
                    if (instruction.IsConstantAssigned(this, value)) return;
                    LoadConstant(instruction, value.ToString());
                    instruction.SetRegisterConstant(this, value);
                    instruction.AddChanged(this);
                    return;
                case PointerOperand sourcePointerOperand:
                    if (instruction.IsConstantAssigned(this, sourcePointerOperand)) return;
                    LoadConstant(instruction, sourcePointerOperand.MemoryAddress());
                    instruction.SetRegisterConstant(this, sourcePointerOperand);
                    instruction.AddChanged(this);
                    return;
                case VariableOperand sourceVariableOperand: {
                        var sourceVariable = sourceVariableOperand.Variable;
                        var sourceOffset = sourceVariableOperand.Offset;
                        var variableRegister = instruction.GetVariableRegister(sourceVariableOperand, r => r.Equals(this)) ??
                                               instruction.GetVariableRegister(sourceVariableOperand);
                        if (variableRegister is PointerRegister sourceRegister) {
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
                        var register = pointer.Register ?? instruction.GetVariableRegister(pointer, 0);
                        if (register is PointerRegister pointerRegister) {
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
                        if (destinationVariable.Register is PointerRegister destinationRegister) {
                            Debug.Assert(destinationOffset == 0);
                            if (!Equals(destinationRegister, this)) {
                                destinationRegister.CopyFrom(instruction, this);
                            }
                            instruction.SetVariableRegister(destinationVariable, destinationOffset, destinationRegister);
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
                        StoreIndirect(instruction, reservation.PointerRegister, destinationOffset);
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public abstract void Add(Instruction instruction, int offset);

        public abstract void CopyFrom(Instruction instruction, PointerRegister sourceRegister);

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
    }
}
