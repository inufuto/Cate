﻿using System;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class WordRegister : Register
    {
        protected WordRegister(int id, string name) : base(id, 2, name) { }
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
                WordPointerRegister wordPointerRegister => Conflicts(wordPointerRegister.WordRegister),
                ByteRegister byteRegister => Contains(byteRegister),
                _ => base.Conflicts(register)
            };
        }

        public override bool Matches(Register register)
        {
            return base.Matches(register) || register is ByteRegister byteRegister && Contains(byteRegister);
        }

        //public abstract void Add(Instruction instruction, int offset);
        //public virtual bool IsAddable() => false;
        //public virtual bool IsIndex() => false;
        //public abstract bool IsOffsetInRange(int offset);
        //public abstract bool IsPointer(int offset);

        //public abstract void LoadConstant(Instruction instruction, string value);
        public virtual void LoadConstant(Instruction instruction, int value)
        {
            if (instruction.IsConstantAssigned(this, value)) {
                instruction.AddChanged(this);
                return;
            }
            LoadConstant(instruction, value.ToString());
            instruction.SetRegisterConstant(this, value);
        }
        //public abstract void LoadFromMemory(Instruction instruction, string label);
        //public abstract void StoreToMemory(Instruction instruction, string label);

        public void Load(Instruction instruction, Operand sourceOperand)
        {
            switch (sourceOperand) {
                case IntegerOperand sourceIntegerOperand:
                    var value = sourceIntegerOperand.IntegerValue;
                    if (instruction.IsConstantAssigned(this, value)) return;
                    LoadConstant(instruction, value);
                    instruction.SetRegisterConstant(this, value);
                    instruction.AddChanged(this);
                    return;
                //case PointerOperand sourcePointerOperand:
                //    if (instruction.IsConstantAssigned(this, sourcePointerOperand)) return;
                //    LoadConstant(instruction, sourcePointerOperand.MemoryAddress());
                //    //instruction.WriteLine("\tld\t" + this + "," + sourcePointerOperand.MemoryAddress());
                //    instruction.SetRegisterConstant(this, sourcePointerOperand);
                //    instruction.AddChanged(this);
                //    return;
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
                        StoreIndirect(instruction, reservation.PointerRegister, destinationOffset);
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        //protected virtual void StoreToMemory(Instruction instruction, Variable variable, int offset)
        //{
        //    StoreToMemory(instruction, variable.MemoryAddress(offset));
        //    instruction.SetVariableRegister(variable, offset, this);
        //}


        //public virtual void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        //{
        //    LoadFromMemory(instruction, variable.MemoryAddress(offset));
        //    instruction.SetVariableRegister(variable, offset, this);
        //    instruction.AddChanged(this);
        //}


        //public abstract void LoadIndirect(Instruction instruction, PointerRegister pointerRegister, int offset);
        //public virtual void LoadIndirect(Instruction instruction, Variable pointer, int offset)
        //{
        //    var candidates = PointerOperation.RegistersToOffset(offset).Where(r => !r.Conflicts(this)).ToList();
        //    if (candidates.Count == 0) {
        //        candidates = PointerOperation.Registers.Where(r => !r.Conflicts(this)).ToList();
        //    }
        //    if (candidates.Count == 0) {
        //        candidates = PointerOperation.Registers;
        //    }
        //    using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
        //    reservation.WordRegister.LoadFromMemory(instruction, pointer, 0);
        //    LoadIndirect(instruction, reservation.PointerRegister, offset);
        //}

        //public abstract void StoreIndirect(Instruction instruction, PointerRegister pointerRegister, int offset);

        public abstract void CopyFrom(Instruction instruction, WordRegister sourceRegister);


        public abstract void Operate(Instruction instruction, string operation, bool change, Operand operand);

        //public virtual void TemporaryOffset(Instruction instruction, int offset, Action action)
        //{
        //    if (instruction.IsRegisterReserved(this)) {
        //        var changed = instruction.IsChanged(this);
        //        Add(instruction, offset);
        //        action();
        //        if (changed) return;
        //        Add(instruction, -offset);
        //        instruction.RemoveChanged(this);
        //    }
        //    else {
        //        var changed = instruction.IsChanged(this);
        //        using var reservation = instruction.ReserveRegister(this);
        //        Add(instruction, offset);
        //        instruction.RemoveRegisterAssignment(this);
        //        instruction.AddChanged(this);
        //        action();
        //        Add(instruction, -offset);
        //        if (!changed) {
        //            instruction.RemoveChanged(this);
        //        }
        //    }
        //}
    }
}