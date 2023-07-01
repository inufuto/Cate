using System;

namespace Inu.Cate
{
    public class RegisterReservation : IDisposable
    {
        public abstract class Saving
        {
            public abstract void Restore(Instruction instruction);
            internal bool Changed;
        }

        public readonly Register Register;
        public readonly Variable? Variable;
        private readonly Instruction instruction;
        private readonly Saving? saving;

        internal RegisterReservation(Register register, Operand? operand, Instruction instruction)
        {
            Register = register;
            this.instruction = instruction;
            Variable = operand switch
            {
                VariableOperand variableOperand => variableOperand.Variable,
                IndirectOperand indirectOperand => indirectOperand.Variable,
                _ => Variable
            };
            if (!instruction.IsRegisterReserved(register, operand)) return;
            saving = Register switch
            {
                ByteRegister byteRegister => this.instruction.ByteOperation.Save(byteRegister, this.instruction),
                WordRegister wordRegister => this.instruction.WordOperation.Save(wordRegister, this.instruction),
                _ => throw new NotImplementedException()
            };
            saving.Changed = instruction.IsChanged(register);
        }

        public ByteRegister ByteRegister => (ByteRegister)Register;
        public WordRegister WordRegister => (WordRegister)Register;
        public PointerRegister PointerRegister => (PointerRegister)Register;

        public void Dispose()
        {
            if (saving != null) {
                saving.Restore(instruction);
                if (!saving.Changed) {
                    instruction.RemoveChanged((Register));
                }
            }
            instruction.CancelRegister(Register);
        }

        public override string ToString()
        {
            return Register.Name + "," + Variable;
        }
    }
}
