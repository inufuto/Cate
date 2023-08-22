using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class PointerOperation : Cate.PointerOperation
    {
        private static PointerOperation? instance;

        public override List<Cate.PointerRegister> Registers => PointerRegister.Registers;

        public PointerOperation()
        {
            instance = this;
        }

        public static void Operate(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand)
        {
            if (destinationOperand.SameStorage(leftOperand)) {
                if (Tms99.Compiler.Operate(instruction, operation, rightOperand, destinationOperand)) return;
            }

            void OperateRegister(Cate.PointerRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + Tms99.Compiler.OperandToString(instruction, rightOperand, false) + "," + register.Name);
                register.Store(instruction, destinationOperand);
            }

            if (destinationOperand.Register is PointerRegister pointerRegister && !Equals(pointerRegister, rightOperand.Register)) {
                OperateRegister(pointerRegister);
                return;
            }
            Debug.Assert(instance != null);
            var candidates = PointerOperation.Registers.Where(r => !Equals(r, rightOperand.Register)).ToList();
            using var reservation = instance.ReserveAnyRegister(instruction, candidates, leftOperand);
            OperateRegister(reservation.PointerRegister);
        }
    }
}
