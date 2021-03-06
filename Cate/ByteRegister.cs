using System;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class ByteRegister : Register
    {
        protected ByteRegister(int id, string name) : base(id, 1, name) { }

        public virtual WordRegister? PairRegister => null;

        //public override bool Contains(Register register)
        //{
        //    return base.Contains(register) || register is ByteRegister byteRegister && Contains(byteRegister);
        //}
        public override bool Conflicts(Register? register)
        {
            if (register is WordRegister wordRegister && wordRegister.Contains(this)) {
                return true;
            }
            return base.Conflicts(register);
        }

        public override bool Matches(Register register)
        {
            if (register is WordRegister wordRegister && wordRegister.Contains(this)) {
                return true;
            }
            return base.Matches(register);
        }


        public abstract void LoadConstant(Instruction instruction, string value);

        public virtual void LoadConstant(Instruction instruction, int value)
        {
            LoadConstant(instruction, value.ToString());
        }

        public abstract void LoadFromMemory(Instruction instruction, Variable variable, int offset);
        public abstract void StoreToMemory(Instruction instruction, Variable variable, int offset);
        public abstract void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset);
        public abstract void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset);

        protected virtual void LoadIndirect(Instruction instruction, Variable pointer, int offset)
        {
            Compiler.Instance.WordOperation.UsingAnyRegister(instruction,
                Compiler.Instance.WordOperation.PointerRegisters(offset),
                pointerRegister =>
                {
                    pointerRegister.LoadFromMemory(instruction, pointer, 0);
                    LoadIndirect(instruction, pointerRegister, offset);
                });
        }

        protected virtual void StoreIndirect(Instruction instruction, Variable pointer, int offset)
        {
            Compiler.Instance.WordOperation.UsingAnyRegister(instruction,
                Compiler.Instance.WordOperation.PointerRegisters(offset),
                pointerRegister =>
                {
                    pointerRegister.LoadFromMemory(instruction, pointer, 0);
                    //instruction.RemoveVariableRegisterId(pointerRegister.Id);
                    StoreIndirect(instruction, pointerRegister, offset);
                });
            //instruction.RemoveVariableRegisterId(Id);
        }


        public virtual void Load(Instruction instruction, Operand sourceOperand)
        {
            switch (sourceOperand) {
                case IntegerOperand integerOperand:
                    LoadConstant(instruction, integerOperand.IntegerValue);
                    return;
                case StringOperand stringOperand:
                    LoadConstant(instruction, stringOperand.StringValue);
                    return;
                case VariableOperand variableOperand: {
                        var register = instruction.GetVariableRegister(variableOperand);
                        if (register is ByteRegister byteRegister) {
                            if (Equals(byteRegister, this)) {
                            }
                            else if (byteRegister.ByteCount == 1) {
                                CopyFrom(instruction, byteRegister);
                                instruction.ChangedRegisters.Add(this);
                            }
                            else {
                                LoadFromMemory(instruction, variableOperand.Variable, variableOperand.Offset);
                                instruction.ChangedRegisters.Add(this);
                            }
                        }
                        else {
                            LoadFromMemory(instruction, variableOperand.Variable, variableOperand.Offset);
                            instruction.ChangedRegisters.Add(this);
                            instruction.RemoveVariableRegister(this);
                        }
                        instruction.SetVariableRegister(variableOperand, this);
                        return;
                    }
                case IndirectOperand sourceIndirectOperand: {
                        var pointer = sourceIndirectOperand.Variable;
                        var offset = sourceIndirectOperand.Offset;
                        var register = instruction.GetVariableRegister(pointer, 0);
                        if (register is WordRegister pointerRegister) {
                            if (pointerRegister.IsPointer(offset)) {
                                LoadIndirect(instruction, pointerRegister, offset);
                                instruction.ChangedRegisters.Add(this);
                                return;
                            }
                            var candidates = WordOperation.Registers.Where(r => r.IsPointer(offset)).ToList();
                            WordOperation.UsingAnyRegister(instruction, candidates, temporaryRegister =>
                            {
                                temporaryRegister.CopyFrom(instruction, pointerRegister);
                                LoadIndirect(instruction, temporaryRegister, offset);
                            });
                            instruction.ChangedRegisters.Add(this);
                            return;
                        }
                        LoadIndirect(instruction, pointer, offset);
                        instruction.ChangedRegisters.Add(this);
                        return;
                    }
                case ByteRegisterOperand byteRegisterOperand:
                    byteRegisterOperand.CopyTo(instruction, this);
                    return;
            }
            throw new NotImplementedException();
        }




        public virtual void Store(Instruction instruction, Operand destinationOperand)
        {
            switch (destinationOperand) {
                case StringOperand stringOperand:
                    StoreToMemory(instruction, stringOperand.StringValue);
                    return;
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        if (variable.Register is ByteRegister register) {
                            Debug.Assert(offset == 0);
                            //var register = Compiler.Instance.ByteOperation.RegisterFromId(variable.Register.Value);
                            if (Equals(register, this)) {
                            }
                            else {
                                register.CopyFrom(instruction, this);
                            }
                        }
                        else {
                            StoreToMemory(instruction, variable, offset);
                        }
                        instruction.SetVariableRegister(variableOperand, this);
                        return;
                    }
                case IndirectOperand destinationIndirectOperand: {
                        var pointer = destinationIndirectOperand.Variable;
                        var offset = destinationIndirectOperand.Offset;
                        var register = instruction.GetVariableRegister(pointer, 0);
                        if (register is WordRegister pointerRegister) {
                            StoreIndirect(instruction, pointerRegister, offset);
                            return;
                        }
                        StoreIndirect(instruction, pointer, offset);
                        return;
                    }
                case ByteRegisterOperand byteRegisterOperand:
                    byteRegisterOperand.CopyFrom(instruction, this);
                    return;
            }
            throw new NotImplementedException();
        }


        public abstract void LoadFromMemory(Instruction instruction, string label);

        public abstract void StoreToMemory(Instruction instruction, string label);


        public abstract void CopyFrom(Instruction instruction, ByteRegister sourceRegister);

        public abstract void Operate(Instruction instruction, string operation, bool change, int count);
        public abstract void Operate(Instruction instruction, string operation, bool change, Operand operand);
        public abstract void Operate(Instruction instruction, string operation, bool change, string operand);

        public virtual void Exchange(Instruction subroutineInstruction, ByteRegister register)
        {
            throw new NotImplementedException();
        }

        public abstract void Save(Instruction instruction);
        public abstract void Restore(Instruction instruction);
    }
}