using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class ByteOperation : Cate.ByteOperation
    {
        private static ByteOperation? instance;
        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
        public override List<Cate.ByteRegister> Accumulators => Registers;

        public ByteOperation()
        {
            instance = this;
        }

        protected override void OperateConstant(Instruction instruction, string operation, string value, int count)
        {
            throw new System.NotImplementedException();
        }

        protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset, int count)
        {
            UsingAnyRegister(instruction, register =>
            {
                register.LoadFromMemory(instruction, variable, offset);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t" + register.Name);
                }
                register.StoreToMemory(instruction, variable, offset);
            });
            if (change) {
                instruction.RemoveVariableRegister(variable, offset);
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.WordRegister pointerRegister, int offset,
            int count)
        {
            UsingAnyRegister(instruction, register =>
            {
                register.LoadIndirect(instruction, pointerRegister, offset);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t" + register.Name);
                }
                register.StoreIndirect(instruction, pointerRegister, offset);
            });
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset, int value)
        {
            var candidates = Registers.Where(r => !r.Conflicts(pointerRegister)).ToList();
            UsingAnyRegister(instruction, candidates, temporaryRegister =>
            {
                temporaryRegister.LoadConstant(instruction, value);
                temporaryRegister.StoreIndirect(instruction, pointerRegister, offset);
            });
        }

        public override void ClearByte(Instruction instruction, string label)
        {
            throw new System.NotImplementedException();
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister rightRegister)
        {
            throw new System.NotImplementedException();
        }

        public static void Operate(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand)
        {
            if (destinationOperand.SameStorage(leftOperand)) {
                if (Compiler.Operate(instruction, operation, rightOperand, destinationOperand)) return;
            }
            Debug.Assert(instance != null);
            var candidates = ByteRegister.Registers.Where(r => !rightOperand.Conflicts(r)).ToList();
            instance.UsingAnyRegisterToChange(instruction, candidates, destinationOperand, leftOperand, destinationRegister =>
            {
                destinationRegister.Load(instruction, leftOperand);
                var right = Compiler.OperandToString(instruction, rightOperand);
                if (right != null) {
                    instruction.WriteLine("\t" + operation + "\t" + right + "," + destinationRegister.Name);
                    instruction.ChangedRegisters.Add(destinationRegister);
                    instruction.RemoveRegisterAssignment(destinationRegister);
                    destinationRegister.Store(instruction, destinationOperand);
                }
                else {
                    instruction.BeginRegister(destinationRegister);
                    instance.UsingAnyRegister(instruction, rightRegister =>
                    {
                        rightRegister.Load(instruction, rightOperand);
                        //destinationRegister.Load(instruction, leftOperand);
                        instruction.WriteLine("\t" + operation + "\t" + rightRegister.Name + "," + destinationRegister.Name);
                        instruction.ChangedRegisters.Add(destinationRegister);
                        instruction.RemoveRegisterAssignment(destinationRegister);
                        destinationRegister.Store(instruction, destinationOperand);
                    });
                    instruction.EndRegister(destinationRegister);
                }
            });
        }

        public static void OperateConstant(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, string value)
        {
            Debug.Assert(instance != null);
            instance.UsingAnyRegisterToChange(instruction, destinationOperand, leftOperand, destinationRegister =>
            {
                destinationRegister.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + destinationRegister.Name + "," + value);
                destinationRegister.Store(instruction, destinationOperand);
                instruction.ChangedRegisters.Add(destinationRegister);
                instruction.RemoveRegisterAssignment(destinationRegister);
            });
        }
        public static void OperateConstant(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, int value)
        {
            OperateConstant(instruction, operation, destinationOperand, leftOperand, value.ToString());
        }

        public static void Operate(Instruction instruction, string operation, Operand leftOperand, Operand rightOperand)
        {
            Debug.Assert(instance != null);

            var left = Compiler.OperandToString(instruction, leftOperand);
            var right = Compiler.OperandToString(instruction, rightOperand);
            if (left != null) {
                if (right != null) {
                    instruction.WriteLine("\t" + operation + "\t" + left + "," + right);
                    return;
                }
                instance.UsingAnyRegister(instruction, rightRegister =>
                {
                    rightRegister.Load(instruction, rightOperand);
                    instruction.WriteLine("\t" + operation + "\t" + left + "," + rightRegister.Name);
                    rightRegister.Store(instruction, rightOperand);
                });
                return;
            }

            void OperateRegister(Cate.ByteRegister register)
            {
                register.Load(instruction, leftOperand);
                if (right != null) {
                    instruction.WriteLine("\t" + operation + "\t" + register.Name + "," +
                                          right);
                }
                else {
                    Debug.Assert(instance != null);
                    instance.UsingAnyRegister(instruction, rightRegister =>
                    {
                        rightRegister.Load(instruction, rightOperand);
                        instruction.WriteLine("\t" + operation + "\t" + register.Name + "," + rightRegister.Name);
                        rightRegister.Store(instruction, rightOperand);
                    });
                }

                if (rightOperand is IndirectOperand indirectOperand && indirectOperand.Variable.Register == null) {
                    var offset = indirectOperand.Offset;
                    var candidates = WordRegister.Registers.Where(r => r.IsOffsetInRange(offset)).ToList();
                    Cate.Compiler.Instance.WordOperation.UsingAnyRegister(instruction, candidates, pointerRegister =>
                    {
                        pointerRegister.LoadFromMemory(instruction, indirectOperand.Variable, 0);
                        if (offset == 0) {
                            instruction.WriteLine("\t" + operation + "\t" + register.Name + ",*" + pointerRegister.Name);
                        }
                        else {
                            instruction.WriteLine("\t" + operation + "\t" + register.Name + ",@" + offset + "(" + pointerRegister.Name + ")");
                        }
                    });
                }
            }

            if (leftOperand.Register is ByteRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            instance.UsingAnyRegister(instruction, OperateRegister);
        }

        public static void OperateConstant(Instruction instruction, string operation, Operand leftOperand, int value)
        {
            void OperateRegister(Cate.ByteRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + register.Name + "," + ByteRegister.ByteConst(value));
            }

            Debug.Assert(instance != null);
            if (leftOperand.Register is ByteRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            instance.UsingAnyRegister(instruction, OperateRegister);
        }
    }
}
