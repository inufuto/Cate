using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class WordOperation : Cate.WordOperation
    {
        private static WordOperation? instance;
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;
        public WordOperation()
        {
            instance = this;
        }

        public static void Operate(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand)
        {
            if (destinationOperand.SameStorage(leftOperand)) {
                if (Tms99.Compiler.Operate(instruction, operation, rightOperand, destinationOperand)) return;
            }

            void OperateRegister(Cate.WordRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + Tms99.Compiler.OperandToString(instruction, rightOperand) + "," + register.Name);
                register.Store(instruction, destinationOperand);
            }

            Debug.Assert(instance != null);
            var candidates = WordRegister.Registers.Where(r=>!Equals(r, rightOperand.Register)).ToList();
            instance.UsingAnyRegister(instruction, candidates, destinationOperand, leftOperand, OperateRegister);
        }

        public static void OperateConstant(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, int value)
        {
            OperateConstant(instruction, operation, destinationOperand, leftOperand, value.ToString());
        }

        public static void OperateConstant(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, string value)
        {
            void OperateRegisterConstant(Cate.WordRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + register.Name + "," + value);
                instruction.ChangedRegisters.Add(register);
                instruction.RemoveRegisterAssignment(register);
                register.Store(instruction, destinationOperand);
            }
            if (destinationOperand.Register is WordRegister destinationRegister) {
                OperateRegisterConstant(destinationRegister);
                return;
            }
            if (leftOperand.Register is WordRegister leftRegister) {
                OperateRegisterConstant(leftRegister);
                return;
            }
            Debug.Assert(instance != null);
            instance.UsingAnyRegister(instruction, OperateRegisterConstant);
        }


        public static void Operate(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand sourceOperand)
        {
            if (destinationOperand.SameStorage(sourceOperand)) {
                if (Tms99.Compiler.Operate(instruction, operation, destinationOperand)) return;
            }

            void OperateRegister(Cate.WordRegister register)
            {
                register.Load(instruction, sourceOperand);
                instruction.WriteLine("\t" + operation + "\t" + register.Name);
                register.Store(instruction, destinationOperand);
            }

            if (destinationOperand.Register is WordRegister destinationRegister) {
                OperateRegister(destinationRegister);
                return;
            }

            if (sourceOperand.Register is WordRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            Debug.Assert(instance != null);
            instance.UsingAnyRegister(instruction, OperateRegister);
        }

        public static void Operate(Instruction instruction, string operation, Operand leftOperand, Operand rightOperand)
        {
            void OperateRegister(Cate.WordRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + register.Name + "," +
                                      Tms99.Compiler.OperandToString(instruction, rightOperand));
            }

            Debug.Assert(instance != null);
            if (leftOperand.Register is WordRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            instance.UsingAnyRegister(instruction, OperateRegister);
        }

        public static void OperateConstant(Instruction instruction, string operation, Operand leftOperand, string value)
        {
            void OperateRegister(Cate.WordRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + register.Name + "," + value);
            }

            Debug.Assert(instance != null);
            if (leftOperand.Register is WordRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            instance.UsingAnyRegister(instruction, OperateRegister);
        }
        public static void OperateConstant(Instruction instruction, string operation, Operand leftOperand, int value)
        {
            OperateConstant(instruction, operation, leftOperand, value.ToString());
        }

        public static void Operate(Instruction instruction, string operation, AssignableOperand operand)
        {
            if (Tms99.Compiler.Operate(instruction, operation, operand)) return;

            void OperateRegister(Cate.WordRegister register)
            {
                register.Load(instruction, operand);
                instruction.WriteLine("\t" + operation + "\t" + register.Name);
                register.Store(instruction, operand);
            }

            if (operand.Register is WordRegister destinationRegister) {
                OperateRegister(destinationRegister);
                return;
            }
            Debug.Assert(instance != null);
            instance.UsingAnyRegister(instruction, OperateRegister);
        }
    }
}
