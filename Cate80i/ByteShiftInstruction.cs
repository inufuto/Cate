using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Cate.I8080
{
    internal class ByteShiftInstruction : Cate.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }


        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            if (
                Equals(register, ByteRegister.A) &&
                !IsOperatorExchangeable() &&
                RightOperand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)
            )
                return false;
            return base.CanAllocateRegister(variable, register);
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            var functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftA",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedA"
                    : "cate.ShiftRightA",
                _ => throw new NotImplementedException()
            };
            ByteOperation.UsingRegister(this, ByteRegister.B, RightOperand, () =>
            {
                ByteRegister.B.Load(this, RightOperand);
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, LeftOperand);
                    Compiler.CallExternal(this, functionName);
                    ByteRegister.A.Store(this, DestinationOperand);
                });
            });
        }

        protected override void ShiftConstant(int count)
        {
            if (count == 0) {
                if (!DestinationOperand.SameStorage(LeftOperand)) {
                    ByteOperation.UsingAnyRegister(this, DestinationOperand, LeftOperand, register =>
                    {
                        register.Load(this, LeftOperand);
                        register.Store(this, DestinationOperand);
                    });
                }
                return;
            }

            if (((IntegerType)LeftOperand.Type).Signed) {
                ShiftVariable(RightOperand);
                return;
            }
            var operation = Operation();

            void OperateA()
            {
                Repeat(() => { WriteLine("\tora\ta | " + operation); }, count);
                ChangedRegisters.Add(ByteRegister.A);
                RemoveRegisterAssignment(ByteRegister.A);
            }

            if (Equals(DestinationOperand.Register, ByteRegister.A)) {
                ByteRegister.A.Load(this, LeftOperand);
                OperateA();
                return;
            }
            ByteOperation.UsingRegister(this, ByteRegister.A,LeftOperand, () =>
            {
                ByteRegister.A.Load(this, LeftOperand);
                OperateA();
                ByteRegister.A.Store(this, DestinationOperand);
            });
        }

        protected override string Operation()
        {
            return OperatorId switch
            {
                Keyword.ShiftLeft => "ral",
                Keyword.ShiftRight => "rar",
                _ => throw new NotImplementedException()
            };
        }
    }
}
