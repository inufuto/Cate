using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class ByteOperation
    {
        public abstract List<ByteRegister> Accumulators { get; }

        protected virtual void OperateConstant(Instruction instruction, string operation, string value, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + value);
            }
        }

        protected abstract void OperateMemory(Instruction instruction, string operation, bool change, Variable variable,
            int offset, int count);

        protected abstract void OperateIndirect(Instruction instruction, string operation, bool change,
            WordRegister pointerRegister, int offset, int count);

        protected virtual void OperateIndirect(Instruction instruction, string operation, bool change, Variable pointer,
            int offset, int count)
        {
            Compiler.Instance.WordOperation.UsingAnyRegister(instruction, Compiler.Instance.WordOperation.PointerRegisters(offset), pointerRegister =>
            {
                pointerRegister.LoadFromMemory(instruction, pointer, 0);
                OperateIndirect(instruction, operation, change, pointerRegister, offset, count);
            });
        }


        public virtual void Operate(Instruction instruction, string operation, bool change, Operand operand,
            int count)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    OperateConstant(instruction, operation, integerOperand.IntegerValue.ToString(), count);
                    return;
                case StringOperand stringOperand:
                    OperateConstant(instruction, operation, stringOperand.StringValue, count);
                    return;
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        var register = variable.Register;
                        if (register is ByteRegister byteRegister) {
                            Debug.Assert(operation.Replace("\t", "").Replace(" ", "").Length == 3);
                            //var register = RegisterFromId(Register);
                            byteRegister.Operate(instruction, operation, change, count);
                            instruction.ChangedRegisters.Remove(byteRegister);
                            instruction.ResultFlags |= Instruction.Flag.Z;
                            return;
                        }
                        OperateMemory(instruction, operation, change, variable, offset, count);
                        return;
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        if (pointer.Register is WordRegister pointerRegister) {
                            //var pointerRegister = Compiler.Instance.WordOperation.RegisterFromId(pointer.Register.Value);
                            OperateIndirect(instruction, operation, change, pointerRegister, offset, count);
                            return;
                        }
                        OperateIndirect(instruction, operation, change, pointer, offset, count);
                        return;
                    }
                case ByteRegisterOperand byteRegisterOperand: {
                        byteRegisterOperand.Register.Operate(instruction, operation, change, count);
                        instruction.ResultFlags |= Instruction.Flag.Z;
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            Operate(instruction, operation, change, operand, 1);
        }

        public abstract void StoreConstantIndirect(Instruction instruction, WordRegister pointerRegister, int offset,
            int value);

        public abstract List<ByteRegister> Registers { get; }


        public ByteRegister TemporaryRegister(Instruction instruction, IEnumerable<ByteRegister> candidates)
        {
            var register = candidates.First(r => !instruction.IsRegisterInUse(r));
            Debug.Assert(register != null);
            return register;
        }

        public void UsingRegister(Instruction instruction, ByteRegister register, Action action)
        {
            if (instruction.IsRegisterInUse(register)) {
                var candidates = Registers.Where(r => !Equals(r, register) && !instruction.IsRegisterInUse(r)).ToList();
                if (candidates.Any()) {
                    UsingAnyRegister(instruction, candidates, otherRegister =>
                    {
                        otherRegister.CopyFrom(instruction, register);
                        action();
                        register.CopyFrom(instruction, otherRegister);
                        instruction.ChangedRegisters.Add(otherRegister);
                        instruction.RemoveRegisterAssignment(otherRegister);
                    });
                    return;
                }
                register.Save(instruction);
                action();
                register.Restore(instruction);
                return;
            }
            instruction.BeginRegister(register);
            action();
            instruction.EndRegister(register);
        }

        public void UsingRegister(Instruction instruction, ByteRegister register, Operand operand, Action action)
        {
            if (Equals(operand.Register, register)) {
                instruction.BeginRegister(operand.Register);
                action();
                instruction.EndRegister(operand.Register);
                return;
            }
            UsingRegister(instruction, register, action);
        }

        public void UsingAnyRegister(Instruction instruction, List<ByteRegister> candidates,
            AssignableOperand? destinationOperand, Operand? sourceOperand, Action<ByteRegister> action)
        {
            {
                if (destinationOperand?.Register is ByteRegister byteRegister && candidates.Contains(byteRegister)) {
                    instruction.BeginRegister(byteRegister);
                    action(byteRegister);
                    instruction.EndRegister(byteRegister);
                    return;
                }
            }
            if (sourceOperand is VariableOperand variableOperand) {
                var register = instruction.GetVariableRegister(variableOperand);
                if (register is ByteRegister byteRegister && candidates.Contains(byteRegister)) {
                    instruction.BeginRegister(byteRegister);
                    action(byteRegister);
                    instruction.EndRegister(byteRegister);
                    return;
                }
            }
            UsingAnyRegister(instruction, candidates, action);
        }

        public void UsingAnyRegister(Instruction instruction, AssignableOperand? destinationOperand,
            Operand? sourceOperand, Action<ByteRegister> action)
        {
            UsingAnyRegister(instruction, Registers, destinationOperand, sourceOperand, action);
        }

        public void UsingAnyRegister(Instruction instruction, List<ByteRegister> candidates,
            Action<ByteRegister> action)
        {
            void Invoke(Cate.ByteRegister register)
            {
                instruction.BeginRegister(register);
                action(register);
                instruction.EndRegister(register);
            }

            if (Compiler.Instance.IsAssignedRegisterPrior()) {
                foreach (var register in candidates.Where(r => !instruction.IsRegisterInUse(r) && !instruction.IsRegisterInVariableRange(r, null))) {
                    Invoke(register);
                    return;
                }
            }
            foreach (var register in candidates.Where(register => !instruction.IsRegisterInUse(register))) {
                Invoke(register);
                return;
            }
            var savedRegister = candidates.Last();
            var changed = instruction.ChangedRegisters.Contains(savedRegister);
            SaveAndRestore(instruction, savedRegister, () => action(savedRegister));
            if (!changed) {
                instruction.ChangedRegisters.Remove(savedRegister);
            }
        }

        protected virtual void SaveAndRestore(Instruction instruction, ByteRegister register, Action action)
        {
            var temporaryRegister = Registers.Find(r => !Equals(r, register) && !instruction.IsRegisterInUse(r));
            if (temporaryRegister != null) {
                temporaryRegister.CopyFrom(instruction, register);
                action();
                register.CopyFrom(instruction, temporaryRegister);
            }
            else {
                register.Save(instruction);
                action();
                register.Restore(instruction);
            }
        }

        public void UsingAnyRegister(Instruction instruction, Action<ByteRegister> action)
        {
            UsingAnyRegister(instruction, Registers, action);
        }

        public void UsingAnyRegisterToChange(Instruction instruction, List<ByteRegister> candidates,
            AssignableOperand destinationOperand, Operand sourceOperand,
            Action<ByteRegister> action)
        {
            if (destinationOperand.Register is ByteRegister destinationRegister) {
                if (candidates.Contains(destinationRegister)) {
                    action(destinationRegister);
                    return;
                }
            }
            if (sourceOperand.Register is ByteRegister sourceRegister && instruction.IsRegisterInUse(sourceOperand.Register)) {
                if (candidates.Contains(sourceRegister)) {
                    instruction.BeginRegister(sourceRegister);
                    action(sourceRegister);
                    instruction.EndRegister(sourceRegister);
                    return;
                }
            }
            UsingAnyRegister(instruction, candidates, action);
        }

        public void UsingAnyRegisterToChange(Instruction instruction, AssignableOperand destinationOperand,
            Operand sourceOperand, Action<ByteRegister> action)
        {
            UsingAnyRegisterToChange(instruction, Registers, destinationOperand, sourceOperand, action);
        }


        public abstract void ClearByte(Instruction instruction, string label);

        //public void UsingAnyRegister(Instruction instruction, List<ByteRegister> candidates, Operand operand,
        //    Action<ByteRegister> action)
        //{
        //    if (operand is VariableOperand variableOperand) {
        //        if (variableOperand.Register is ByteRegister register) {
        //            if (candidates.Contains(register)) {
        //                action(register);
        //                return;
        //            }
        //        }
        //    }
        //    UsingAnyRegister(instruction, candidates, action);
        //}

        public void OperateByteBinomial(BinomialInstruction instruction, string operation, bool change)
        {
            instruction.ByteOperation.UsingAnyRegisterToChange(instruction, Accumulators, instruction.DestinationOperand, instruction.LeftOperand, register =>
             {
                 if (instruction.RightOperand.Register is ByteRegister rightRegister && Equals(rightRegister, register)) {
                     var temporaryByte = ToTemporaryByte(instruction, rightRegister);
                     register.Load(instruction, instruction.LeftOperand);
                     register.Operate(instruction, operation, change, temporaryByte);
                 }
                 else {
                     register.Load(instruction, instruction.LeftOperand);
                     register.Operate(instruction, operation, change, instruction.RightOperand);
                 }

                 instruction.RemoveRegisterAssignment(register);
                 register.Store(instruction, instruction.DestinationOperand);
             });
        }

        public abstract string ToTemporaryByte(Instruction instruction, ByteRegister register);

        public List<ByteRegister> RegistersOtherThan(Cate.ByteRegister register)
        {
            return Registers.FindAll(r => !Equals(r, register));
        }
    }
}
