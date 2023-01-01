using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class WordOperation
    {
        public Compiler Compiler => Compiler.Instance;

        public abstract List<WordRegister> Registers { get; }

        public List<WordRegister> PairRegisters => Registers.Where(r => r.IsPair()).ToList();

        public List<WordRegister> PointerRegisters(int offset)
        {
            return Registers.Where(r => r.IsPointer(offset)).ToList();
        }

        public virtual void UsingRegister(Instruction instruction, WordRegister register, Action action)
        {
            if (instruction.IsRegisterInUse(register)) {
                var candidates = Registers.Where(
                    r => !Equals(r, register) &&
                    CanCopyRegisterToSave(instruction, r)
                ).ToList();
                if (candidates.Any()) {
                    UsingAnyRegister(instruction, candidates, otherRegister =>
                    {
                        otherRegister.CopyFrom(instruction, register);
                        action();
                        register.CopyFrom(instruction, otherRegister);
                    });
                }
                else {
                    register.Save(instruction);
                    action();
                    register.Restore(instruction);
                }

                return;
            }
            instruction.BeginRegister(register);
            action();
            instruction.EndRegister(register);
        }

        protected virtual bool CanCopyRegisterToSave(Instruction instruction, WordRegister register)
        {
            return ((instruction.ResultOperand == null || !Equals(register, instruction.ResultOperand.Register)) &&
                    !instruction.IsRegisterInVariableRange(register, null));
        }

        public void UsingRegister(Instruction instruction, WordRegister register, Operand operand, Action action)
        {
            if (Equals(operand.Register, register)) {
                action();
                return;
            }
            UsingRegister(instruction, register, action);
        }

        public WordRegister TemporaryRegister(Instruction instruction, List<WordRegister> candidates)
        {
            var register = candidates.First(r => !instruction.IsRegisterInUse(r));
            Debug.Assert(register != null);
            return register;
        }

        public void UsingAnyRegister(Instruction instruction, List<WordRegister> candidates,
            AssignableOperand? destinationOperand, Operand? sourceOperand, Action<WordRegister> action)
        {
            {
                if (destinationOperand?.Register is WordRegister wordRegister && candidates.Contains(wordRegister)) {
                    instruction.BeginRegister(wordRegister);
                    action(wordRegister);
                    instruction.EndRegister(wordRegister);
                    return;
                }
            }
            if (sourceOperand is VariableOperand variableOperand) {
                var register = instruction.GetVariableRegister(variableOperand);
                if (register is WordRegister wordRegister && candidates.Contains(register)) {
                    instruction.BeginRegister(wordRegister);
                    action(wordRegister);
                    instruction.EndRegister(wordRegister);
                    return;
                }
            }
            UsingAnyRegister(instruction, candidates, action);
        }

        public void UsingAnyRegister(Instruction instruction, AssignableOperand? destinationOperand,
            Operand? sourceOperand, Action<WordRegister> action)
        {
            UsingAnyRegister(instruction, Registers, destinationOperand, sourceOperand, action);
        }

        public void UsingAnyRegister(Instruction instruction, List<WordRegister> candidates,
            Action<WordRegister> action)
        {
            void Invoke(WordRegister register)
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
            savedRegister.Save(instruction);
            Invoke(savedRegister);
            savedRegister.Restore(instruction);
            if (!changed) {
                instruction.ChangedRegisters.Remove(savedRegister);
            }
        }

        public void UsingAnyRegister(Instruction instruction, Action<Cate.WordRegister> action)
        {
            UsingAnyRegister(instruction, Registers, action);
        }

        public Operand LowByteOperand(Operand operand) => Compiler.LowByteOperand(operand);
        //public abstract void Operate(Instruction instruction, string operation, bool change, Operand operand);
    }
}
