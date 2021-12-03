using System;

namespace Inu.Cate.Z80
{
    internal class ByteShiftInstruction : Cate.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

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

        protected override string Operation()
        {
            return OperatorId switch
            {
                Keyword.ShiftLeft => "sla\t",
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => "sra\t",
                Keyword.ShiftRight => "srl\t",
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
            ByteRegister.Using(this, ByteRegister.B, RightOperand, () =>
            {
                ByteRegister.B.Load(this, RightOperand);
                ByteRegister.UsingAccumulator(this, () =>
                {
                    ByteRegister.A.Load(this, LeftOperand);
                    Compiler.CallExternal(this, functionName);
                    ByteRegister.A.Store(this, DestinationOperand);
                });
            });
        }
    }
}