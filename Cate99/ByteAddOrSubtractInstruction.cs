using System;

namespace Inu.Cate.Tms99
{
    internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
                ExchangeOperands();
            }

            ResultFlags |= Flag.Z;
            if (RightOperand is IntegerOperand integerOperand) {
                var value = OperatorId switch
                {
                    '+' => integerOperand.IntegerValue,
                    '-' => -integerOperand.IntegerValue,
                    _ => throw new NotImplementedException()
                };

                Tms99.ByteOperation.OperateConstant(this, "ai", DestinationOperand, LeftOperand, ByteRegister.ByteConst(value));
                return;
            }
            {
                var operation = OperatorId switch
                {
                    '+' => "ab",
                    '-' => "sb",
                    _ => throw new NotImplementedException()
                };
                Tms99.ByteOperation.Operate(this, operation, DestinationOperand, LeftOperand, RightOperand);
            }
        }

        protected override int Threshold() => 2;

        protected override void Increment(int count)
        {
            ByteOperation.UsingAnyRegisterToChange(this, DestinationOperand, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, "inc", true, count);
                temporaryRegister.Store(this, DestinationOperand);
            });
        }

        protected override void Decrement(int count)
        {
            ByteOperation.UsingAnyRegisterToChange(this, DestinationOperand, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, "dec", true, count);
                temporaryRegister.Store(this, DestinationOperand);
            });
        }
    }
}
