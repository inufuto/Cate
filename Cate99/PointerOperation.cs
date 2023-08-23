using Microsoft.Win32;
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

            if (rightOperand.Register is WordRegister rightRegister && leftOperand is PointerOperand leftPointerOperand) {
                instruction.WriteLine("\t" + operation + "i\t" + rightRegister.AsmName + "," + leftPointerOperand.MemoryAddress());
                var rightPointerRegister = rightRegister.ToPointer();
                Debug.Assert(rightPointerRegister != null);
                rightPointerRegister.Store(instruction, destinationOperand);
                return;
            }
            Debug.Assert(instance != null);
            var candidates = PointerOperation.Registers.Where(r => !Equals(r, rightOperand.Register)).ToList();
            using var reservation = instance.ReserveAnyRegister(instruction, candidates, leftOperand);
            OperateRegister(reservation.PointerRegister);
        }

        public static void Operate(Instruction instruction, string operation, Operand leftOperand, Operand rightOperand)
        {
            void OperateRegister(Cate.PointerRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + register.Name + "," +
                                      Tms99.Compiler.OperandToString(instruction, rightOperand, true));
            }

            Debug.Assert(instance != null);
            if (leftOperand.Register is PointerRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            using var reservation = instance.ReserveAnyRegister(instruction);
            OperateRegister(reservation.PointerRegister);
        }

        public static void OperateConstant(Instruction instruction, string operation, Operand leftOperand, string value)
        {
            void OperateRegister(Cate.PointerRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + register.AsmName + "," + value);
            }

            Debug.Assert(instance != null);
            if (leftOperand.Register is PointerRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            using var reservation = instance.ReserveAnyRegister(instruction);
            OperateRegister(reservation.PointerRegister);
        }
    }
}
