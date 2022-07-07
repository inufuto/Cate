using System;

namespace Inu.Cate.Mc6809
{
    internal class WordAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public WordAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        protected override int Threshold() => 0;

        public override void BuildAssembly()
        {
            if (LeftOperand is ConstantOperand && !(RightOperand is ConstantOperand) && IsOperatorExchangeable()) {
                ExchangeOperands();
            }
            else if (!Equals(LeftOperand.Register, WordRegister.D) && Equals(RightOperand.Register, WordRegister.D)) {
                ExchangeOperands();
            }

            if (AddConstant())
                return;

            string operation = OperatorId switch
            {
                '+' => "add",
                '-' => "sub",
                _ => throw new NotImplementedException()
            };

            void AddD()
            {
                WordRegister.D.Load(this, LeftOperand);
                WordRegister.D.Operate(this, operation, true, RightOperand);
                WordRegister.D.Store(this, DestinationOperand);
                ResultFlags |= Flag.Z;
            }
            if (Equals(LeftOperand.Register, WordRegister.D) && DestinationOperand.Register == WordRegister.D) {
                AddD();
                return;
            }
            WordOperation.UsingRegister(this, WordRegister.D, AddD);
        }

        private bool AddConstant()
        {
            if (!(RightOperand is IntegerOperand integerOperand))
                return false;
            if (Equals(LeftOperand.Register, WordRegister.D) && Equals(DestinationOperand.Register, WordRegister.D))
                return false;

            var value = integerOperand.IntegerValue;
            if (OperatorId == '-') {
                value = -value;
            }

            WordOperation.UsingAnyRegister(this, DestinationOperand, LeftOperand, register =>
            {
                register.Load(this, LeftOperand);
                if (register == WordRegister.D) {
                    WriteLine("\taddd\t#" + value);
                }
                else {
                    WriteLine("\tlea" + register + "\t" + value + "," + register);
                }
                ChangedRegisters.Add(register);
                RemoveRegisterAssignment(register);
                register.Store(this, DestinationOperand);
            });
            return true;
        }


        protected override void Increment(int count)
        {
            throw new NotImplementedException();
        }

        protected override void Decrement(int count)
        {
            throw new NotImplementedException();
        }
    }
}