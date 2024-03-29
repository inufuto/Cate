﻿using System;

namespace Inu.Cate.MuCom87
{
    internal abstract class ByteShiftInstruction : Cate.ByteShiftInstruction
    {
        protected ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftConstant(int count)
        {
            if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                    ByteRegister.B.LoadConstant(this, count);
                    CallExternal("cate.ShiftRightSignedA");
                }
                return;
            }

            var operation = Operation();
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, LeftOperand);
                for (var i = 0; i < count; ++i) {
                    WriteLine("\t" + operation);
                }
                ByteRegister.A.Store(this, DestinationOperand);
            }
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
            using (ByteOperation.ReserveRegister(this, ByteRegister.B, RightOperand)) {
                ByteRegister.B.Load(this, RightOperand);
                CallExternal(functionName);
            }
        }

        //public override bool IsRegisterInUse(Register register)
        //{
        //    return !Equals(DestinationOperand.Register, register) && base.IsRegisterInUse(register);
        //}

        private void CallExternal(string functionName)
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, LeftOperand);
                Compiler.CallExternal(this, functionName);
                RemoveRegisterAssignment(ByteRegister.A);
                AddChanged(ByteRegister.A);
                RemoveRegisterAssignment(ByteRegister.B);
                AddChanged(ByteRegister.B);
                ByteRegister.A.Store(this, DestinationOperand);
            }
        }
    }
}
