using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate
{
    public abstract class WordOperation
    {
        private class Saving : RegisterReservation.Saving
        {
            private readonly Cate.WordRegister register;

            public Saving(Cate.WordRegister register, Instruction instruction, WordOperation wordOperation)
            {
                this.register = register;
                register.Save(instruction);
            }

            public override void Restore(Instruction instruction)
            {
                register.Restore(instruction);
            }
        }
        protected static ByteOperation ByteOperation => Compiler.Instance.ByteOperation;

        public Compiler Compiler => Compiler.Instance;

        public abstract List<WordRegister> Registers { get; }

        public List<WordRegister> PairRegisters => Registers.Where(r => r.IsPair()).ToList();

        //public virtual List<WordRegister> PointerRegisters(int offset)
        //{
        //    return Registers.Where(r => r.IsPointer(offset)).ToList();
        //}

        public virtual RegisterReservation ReserveRegister(Instruction instruction, WordRegister register)
        {
            return instruction.ReserveRegister(register);
        }

        protected virtual bool CanCopyRegisterToSave(Instruction instruction, WordRegister register)
        {
            return ((instruction.ResultOperand == null || !Equals(register, instruction.ResultOperand.Register)) &&
                    !instruction.IsRegisterInVariableRange(register, null));
        }

        public RegisterReservation ReserveRegister(Instruction instruction, WordRegister register, Operand operand)
        {
            instruction.CancelOperandRegister(operand);
            return instruction.ReserveRegister(register, operand);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<WordRegister> candidates, Operand sourceOperand)
        {
            if (sourceOperand is not VariableOperand variableOperand) return ReserveAnyRegister(instruction, candidates);
            {
                var register = instruction.GetVariableRegister(variableOperand);
                if (register is not WordRegister wordRegister || !candidates.Contains(register))
                    return ReserveAnyRegister(instruction, candidates);
                return ReserveRegister(instruction, wordRegister, sourceOperand);
            }
        }


        public RegisterReservation ReserveAnyRegister(Instruction instruction, Operand sourceOperand)
        {
            return ReserveAnyRegister(instruction, Registers, sourceOperand);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<WordRegister> candidates)
        {
            if (Compiler.Instance.IsAssignedRegisterPrior()) {
                foreach (var register in candidates.Where(r => !instruction.IsRegisterReserved(r) && !instruction.IsRegisterInVariableRange(r, null))) {
                    return instruction.ReserveRegister(register);
                }
            }
            foreach (var register in candidates.Where(register => !instruction.IsRegisterReserved(register))) {
                return instruction.ReserveRegister(register);
            }

            var savedRegister = candidates.Last();
            return instruction.ReserveRegister(savedRegister);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction)
        {
            return ReserveAnyRegister(instruction, Registers);
        }

        public Operand LowByteOperand(Operand operand) => Compiler.LowByteOperand(operand);

        public RegisterReservation.Saving Save(WordRegister register, Instruction instruction)
        {
            return new Saving(register, instruction, this);
        }

        public List<WordRegister> RegistersOtherThan(WordRegister wordRegister)
        {
            return Registers.Where(r => !Equals(r, wordRegister)).ToList();
        }
    }
}
