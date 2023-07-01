using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate
{
    public abstract class PointerOperation
    {
        public abstract List<PointerRegister> Registers { get; }

        public List<PointerRegister> RegistersToOffset(int offset)
        {
            return Registers.Where(r => r.IsOffsetInRange(offset)).ToList(); ;
        }

        public RegisterReservation ReserveRegister(Instruction instruction, PointerRegister register)
        {
            return instruction.ReserveRegister(register);
        }

        public RegisterReservation ReserveRegister(Instruction instruction, PointerRegister register, Operand operand)
        {
            instruction.CancelOperandRegister(operand);
            return instruction.ReserveRegister(register, operand);
        }


        public RegisterReservation ReserveAnyRegister(Instruction instruction)
        {
            return ReserveAnyRegister(instruction, Registers);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, Operand sourceOperand)
        {
            return ReserveAnyRegister(instruction, Registers, sourceOperand);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<PointerRegister> candidates)
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

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<PointerRegister> candidates, Operand sourceOperand)
        {
            if (sourceOperand is not VariableOperand variableOperand) return ReserveAnyRegister(instruction, candidates);
            {
                var register = instruction.GetVariableRegister(variableOperand);
                if (register is not PointerRegister pointerRegister || !candidates.Contains(register))
                    return ReserveAnyRegister(instruction, candidates);
                return ReserveRegister(instruction, pointerRegister, sourceOperand);
            }
        }
    }
}
