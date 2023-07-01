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
            if (Equals(LeftOperand.Register, WordRegister.D) && DestinationOperand.Register == WordRegister.D) {
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
            if (RightOperand is ConstantOperand constantOperand && constantOperand.Type is PointerType) {
                return AddConstant(constantOperand.MemoryAddress());
            }
            return false;
        }

        private bool AddConstant(string value)
        {
            Action<Register> load;
            if (LeftOperand.Type is PointerType) {
                load = r => ((PointerRegister)r).Load(this, LeftOperand);
            }
            else {
                load = r =>
                {
                    var wordRegister = r is PointerRegister pointerRegister ? pointerRegister.WordRegister: ((WordRegister)r);
                    wordRegister.Load(this, LeftOperand);
                };
            }

            void ViaRegister(Register r)
            {
                load(r);
                if (Equals(r, WordRegister.D) || Equals(r, PointerRegister.D)) {
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
                    //wordRegister.Load(this, LeftOperand);
                    ViaRegister(wordRegister);
                    return true;
                case PointerRegister pointerRegister when RightOperand.Conflicts(pointerRegister):
                    //pointerRegister.Load(this, LeftOperand);
                    ViaRegister(pointerRegister);
                    return true;
            }

            if (DestinationOperand.Type is PointerType) {
                using var reservation = PointerOperation.ReserveAnyRegister(this, LeftOperand);
                var register = reservation.PointerRegister;
                ViaRegister(register);
                register.Store(this, DestinationOperand);
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
                var register = reservation.WordRegister;
                ViaRegister(register);
                register.Store(this, DestinationOperand);
            }

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