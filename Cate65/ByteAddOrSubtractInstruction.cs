using System;
using System.Collections.Generic;

namespace Inu.Cate.Mos6502
{

    internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override void BuildAssembly()
        {
            if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
                ExchangeOperands();
            }
            if (CanIncrementOrDecrement() && IncrementOrDecrement())
                return;

            string operation = OperatorId switch
            {
                '+' => "clc|adc",
                '-' => "sec|sbc",
                _ => throw new NotImplementedException()
            };
            ResultFlags |= Flag.Z;

            ByteOperation.OperateByteBinomial(this, operation, true);
        }

        private bool CanIncrementOrDecrement()
        {
            if (LeftOperand is IndirectOperand)
                return false;
            if (DestinationOperand is IndirectOperand)
                return false;
            if (Equals(LeftOperand.Register, ByteRegister.A))
                return false;
            if (Equals(DestinationOperand.Register, ByteRegister.A))
                return false;
            return true;
        }


        protected override int Threshold() => 1;
        protected override void Increment(int count)
        {
            IncrementOrDecrement("in", "inc", count);
            ResultFlags |= Flag.Z;
        }

        protected override void Decrement(int count)
        {
            IncrementOrDecrement("de", "dec", count);
            ResultFlags |= Flag.Z;
        }

        private void IncrementOrDecrement(string registerOperation, string memoryOperation, int count)
        {
            if (DestinationOperand.SameStorage(LeftOperand)) {
                OperateByte(memoryOperation, count);
                return;
            }

            var candidates = new List<Cate.ByteRegister>() { ByteRegister.X, ByteRegister.Y };
            ByteOperation.UsingAnyRegister(this, candidates, DestinationOperand, LeftOperand, register =>
            {
                register.Load(this, LeftOperand);
                for (var i = 0; i < count; ++i) {
                    WriteLine("\t" + registerOperation + register);
                }
                register.Store(this, DestinationOperand);
                RemoveRegisterAssignment(register);
                ChangedRegisters.Add(register);
            });
        }
    }
}