using System;

namespace Inu.Cate.I8080
{
    internal class ByteBitInstruction : BinomialInstruction
    {
        public ByteBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override int? RegisterAdaptability(Variable variable, Register register)
        {
            if (
                Equals(register, ByteRegister.A) &&
                !IsOperatorExchangeable() &&
                RightOperand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)
            )
                return null;
            return base.RegisterAdaptability(variable, register);
        }

        public override void BuildAssembly()
        {
            if (Equals(RightOperand.Register, ByteRegister.A) && !Equals(LeftOperand.Register, ByteRegister.A) && IsOperatorExchangeable()) {
                ExchangeOperands();
            }

            var operation = OperatorId switch
            {
                '|' => "ora|ori",
                '^' => "xra|xri",
                '&' => "ana|ani",
                _ => throw new NotImplementedException()
            };
            ResultFlags |= Flag.Z;

            ByteOperation.OperateByteBinomial(this, operation, true);
        }
    }
}
