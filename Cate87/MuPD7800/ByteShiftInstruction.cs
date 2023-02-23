using System;

namespace Inu.Cate.MuCom87.MuPD7800
{
    internal class ByteShiftInstruction:MuCom87.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }
        public override void BuildAssembly()
        {
            if (Equals(LeftOperand.Register, ByteRegister.C) && Equals(DestinationOperand.Register, ByteRegister.C) && RightOperand is IntegerOperand integerOperand && !((IntegerType)LeftOperand.Type).Signed) {
                string operation = OperatorId switch
                {
                    Keyword.ShiftLeft => "shcl",
                    Keyword.ShiftRight => "shcr",
                    _ => throw new NotImplementedException()
                };
                for (var i = 0; i < integerOperand.IntegerValue; ++i) {
                    WriteLine("\t" + operation);
                }
                return;
            }
            base.BuildAssembly();
        }

        protected override string Operation()
        {
            return OperatorId switch
            {
                Keyword.ShiftLeft => "shal",
                Keyword.ShiftRight when !((IntegerType)LeftOperand.Type).Signed => "shar",
                _ => throw new NotImplementedException()
            };
        }
    }
}
