using System;
using System.Diagnostics;

namespace Inu.Cate.Z80
{
    internal class WordBitInstruction : BinomialInstruction
    {
        public WordBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand,
            Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            if (register is WordRegister wordRegister) {
                if (!wordRegister.IsPair())
                    return false;
            }
            return base.CanAllocateRegister(variable, register);
        }

        public override void BuildAssembly()
        {
            if (LeftOperand is ConstantOperand && !(RightOperand is ConstantOperand)) {
                ExchangeOperands();
            }

            var operation = OperatorId switch
            {
                '|' => "or\t",
                '^' => "xor\t",
                '&' => "and\t",
                _ => throw new ArgumentException(OperatorId.ToString())
            };

            using var destinationReservation = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters, DestinationOperand, null);
            var destinationRegister = destinationReservation.WordRegister;
            using (var leftReservation = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters, null, LeftOperand)) {
                var leftRegister = leftReservation.WordRegister;
                leftRegister.Load(this, LeftOperand);
                if (RightOperand is IntegerOperand integerOperand) {
                    var value = integerOperand.IntegerValue;
                    Operate(operation, destinationRegister, leftRegister, "low " + value, "high " + value);
                    return;
                }
                using (var rightReservation = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters, null, RightOperand)) {
                    var rightRegister = rightReservation.WordRegister;
                    rightRegister.Load(this, RightOperand);
                    Debug.Assert(rightRegister.Low != null && rightRegister.High != null);
                    Operate(operation, destinationRegister, leftRegister, rightRegister.Low.Name, rightRegister.High.Name);
                }
            }
            destinationRegister.Store(this, DestinationOperand);
        }

        private void Operate(string operation, Cate.WordRegister destinationRegister, Cate.WordRegister leftRegister,
            string rightLow, string rightHigh)
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                Debug.Assert(leftRegister.Low != null && leftRegister.High != null);
                Debug.Assert(destinationRegister.Low != null && destinationRegister.High != null);
                ByteRegister.A.CopyFrom(this, leftRegister.Low);
                WriteLine("\t" + operation + rightLow);
                destinationRegister.Low.CopyFrom(this, ByteRegister.A);
                ByteRegister.A.CopyFrom(this, leftRegister.High);
                WriteLine("\t" + operation + rightHigh);
                destinationRegister.High.CopyFrom(this, ByteRegister.A);
            }
        }
    }
}


