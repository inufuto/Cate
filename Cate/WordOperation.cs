using System;
using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate
{
    public abstract class WordOperation
    {
        private class Saving : RegisterReservation.Saving
        {
            private readonly Cate.WordRegister register;
            //private RegisterReservation? reservation;

            public Saving(Cate.WordRegister register, Instruction instruction, WordOperation wordOperation)
            {
                this.register = register;
                //var candidates = wordOperation.Registers
                //    .Where(r => !Equals(r, register) && !instruction.IsRegisterReserved(r)).ToList();
                //if (candidates.Any()) {
                //    reservation = wordOperation.ReserveAnyRegister(instruction, candidates);
                //    reservation.WordRegister.CopyFrom(instruction, register);
                //}
                //else {
                register.Save(instruction);
                //}
            }

            public override void Restore(Instruction instruction)
            {
                //if (reservation != null) {
                //    register.CopyFrom(instruction, reservation.WordRegister);
                //    reservation.Dispose();
                //    reservation = null;
                //}
                //else {
                register.Restore(instruction);
                //}
            }
        }
        protected static ByteOperation ByteOperation => Compiler.Instance.ByteOperation;

        public Compiler Compiler => Compiler.Instance;

        public abstract List<WordRegister> Registers { get; }

        public List<WordRegister> PairRegisters => Registers.Where(r => r.IsPair()).ToList();

        public List<WordRegister> PointerRegisters(int offset)
        {
            return Registers.Where(r => r.IsPointer(offset)).ToList();
        }

        public virtual RegisterReservation ReserveRegister(Instruction instruction, WordRegister register)
        {
            //if (instruction.IsRegisterReserved(register)) {
            //    var candidates = Registers.Where(
            //        r => !Equals(r, register) &&
            //        CanCopyRegisterToSave(instruction, r)
            //    ).ToList();
            //    if (candidates.Any()) {
            //        return ReserveAnyRegister(instruction, candidates);
            //    }
            //    else {
            //        register.Save(instruction);
            //        action();
            //        register.Restore(instruction);
            //    }
            //    return;
            //}
            return instruction.ReserveRegister(register);
        }

        protected virtual bool CanCopyRegisterToSave(Instruction instruction, WordRegister register)
        {
            return ((instruction.ResultOperand == null || !Equals(register, instruction.ResultOperand.Register)) &&
                    !instruction.IsRegisterInVariableRange(register, null));
        }

        public RegisterReservation ReserveRegister(Instruction instruction, WordRegister register, Operand operand)
        {
            //if (Equals(operand.Register, register)) {
            instruction.CancelOperandRegister(operand);
            //}
            return instruction.ReserveRegister(register, operand);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<WordRegister> candidates, Operand sourceOperand)
        {
            if (!(sourceOperand is VariableOperand variableOperand)) return ReserveAnyRegister(instruction, candidates);
            {
                var register = instruction.GetVariableRegister(variableOperand);
                if (!(register is WordRegister wordRegister) || !candidates.Contains(register))
                    return ReserveAnyRegister(instruction, candidates);
                //instruction.CancelOperandRegister(sourceOperand);
                return ReserveRegister(instruction, wordRegister, sourceOperand);
            }
        }


        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<WordRegister> candidates,
            AssignableOperand? destinationOperand, Operand sourceOperand)
        {
            return ReserveAnyRegister(instruction, candidates, sourceOperand);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, Operand sourceOperand)
        {
            return ReserveAnyRegister(instruction, Registers, sourceOperand);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, AssignableOperand? destinationOperand,
            Operand sourceOperand)
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
            //var changed = instruction.ChangedRegisters.Contains(savedRegister);
            //savedRegister.Save(instruction);
            //instruction.ReserveRegister(savedRegister);
            //savedRegister.Restore(instruction);
            //if (!changed) {
            //    instruction.ChangedRegisters.Remove(savedRegister);
            //}
            return instruction.ReserveRegister(savedRegister);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction)
        {
            return ReserveAnyRegister(instruction, Registers);
        }

        public Operand LowByteOperand(Operand operand) => Compiler.LowByteOperand(operand);
        //public abstract void Operate(Instruction instruction, string operation, bool change, Operand operand);
        public RegisterReservation.Saving Save(WordRegister register, Instruction instruction)
        {
            return new Saving(register, instruction, this);
        }

    }
}
