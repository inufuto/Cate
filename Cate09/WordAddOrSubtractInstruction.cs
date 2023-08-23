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
            if (LeftOperand is ConstantOperand && RightOperand is not ConstantOperand && IsOperatorExchangeable()) {
                ExchangeOperands();
            }
            else if (!Equals(LeftOperand.Register, WordRegister.D) && Equals(RightOperand.Register, WordRegister.D)) {
                ExchangeOperands();
            }

            if (AddConstant())
                return;

            var operation = OperatorId switch
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
            if (Equals(LeftOperand.Register, WordRegister.D) && Equals(DestinationOperand.Register, WordRegister.D)) {
                AddD();
                return;
            }
            using (WordOperation.ReserveRegister(this, WordRegister.D)) {
                AddD();
            }
        }

        private bool AddConstant()
        {
            if (RightOperand is IntegerOperand integerOperand) {
                if (Equals(LeftOperand.Register, WordRegister.D) && Equals(DestinationOperand.Register, WordRegister.D))
                    return false;

                var value = integerOperand.IntegerValue;
                if (OperatorId == '-') {
                    value = -value;
                }

                {
                    return AddConstant(value.ToString());
                }
            }
            return false;
        }

        private bool AddConstant(string value)
        {
            void ViaRegister(Cate.WordRegister r)
            {
                r.Load(this, LeftOperand);
                if (Equals(r, WordRegister.D)) {
                    WriteLine("\taddd\t#" + value);
                }
                else {
                    WriteLine("\tlea" + r + "\t" + value + "," + r);
                }

                AddChanged(r);
                RemoveRegisterAssignment(r);
            }

            switch (DestinationOperand.Register) {
                case WordRegister wordRegister when RightOperand.Conflicts(wordRegister):
                    ViaRegister(wordRegister);
                    return true;
            }

            using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
            ViaRegister(reservation.WordRegister);
            reservation.WordRegister.Store(this, DestinationOperand);

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