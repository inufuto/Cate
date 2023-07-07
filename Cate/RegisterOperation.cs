using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate
{
    public abstract class RegisterOperation<T> where T:Register
    {
        public static Compiler Compiler => Compiler.Instance;
        public static ByteOperation ByteOperation => Compiler.Instance.ByteOperation;
        public static WordOperation WordOperation => Compiler.Instance.WordOperation;
        public static PointerOperation PointerOperation => Compiler.Instance.PointerOperation;

        public abstract List<T> Registers { get; }


        protected class Saving : RegisterReservation.Saving
        {
            private readonly T register;

            public Saving(T register, Instruction instruction, RegisterOperation<T> registerOperation)
            {
                this.register = register;
                register.Save(instruction);
            }

            public override void Restore(Instruction instruction)
            {
                register.Restore(instruction);
            }
        }

        public RegisterReservation.Saving Save(T register, Instruction instruction)
        {
            return new Saving(register, instruction, this);
        }

        public List<T> RegistersOtherThan(T register)
        {
            return Registers.Where(r => !Equals(r, register)).ToList();
        }

    }
}
