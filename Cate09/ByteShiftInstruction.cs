using System;

namespace Inu.Cate.Mc6809
{
    internal class ByteShiftInstruction : Cate.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand)
            : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override string Operation()
        {
            return OperatorId switch
            {
                Keyword.ShiftLeft => "asl",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed ? "asr" : "lsr",
                _ => throw new NotImplementedException()
            };
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            string functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftA",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedA"
                    : "cate.ShiftRightA",
                _ => throw new NotImplementedException()
            };
            ByteOperation.UsingRegister(this, ByteRegister.B, () =>
           {
               void CallShift()
               {
                   ByteRegister.A.Load(this, LeftOperand);
                   Compiler.CallExternal(this, functionName);
                   RemoveRegisterAssignment(ByteRegister.A);
                   ChangedRegisters.Add(ByteRegister.A);
                   ByteRegister.A.Store(this, DestinationOperand);
               }
               ByteRegister.B.Load(this, RightOperand);
               if (Equals(DestinationOperand.Register, ByteRegister.A)) {
                   CallShift();
               }
               else {
                   ByteOperation.UsingRegister(this, ByteRegister.A, CallShift);
               }
           });
        }
    }
}