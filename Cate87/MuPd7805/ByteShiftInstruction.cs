using System;

namespace Inu.Cate.MuCom87.MuPd7805
{
    internal class ByteShiftInstruction:MuCom87.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }
        protected override string Operation()
        {
            return OperatorId switch
            {
                Keyword.ShiftLeft => "clc|ral",
                Keyword.ShiftRight when !((IntegerType)LeftOperand.Type).Signed => "clc|rar",
                _ => throw new NotImplementedException()
            };
        }
    }
}
