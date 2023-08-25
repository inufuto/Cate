using System;
using System.Diagnostics;

namespace Inu.Cate.MuCom87
{
    internal class PointerAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public PointerAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override void BuildAssembly()
        {
            if (LeftOperand.Type is not PointerType) {
                ExchangeOperands();
            }
            if (IncrementOrDecrement())
                return;

            string lowOperation, highOperation;
            switch (OperatorId) {
                case '+':
                    lowOperation = "add|adi";
                    highOperation = "adc|aci";
                    break;
                case '-':
                    lowOperation = "sub|sui";
                    highOperation = "sbb|sbi";
                    break;
                default:
                    throw new NotImplementedException();
            }

            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, lowOperation, true, Compiler.LowByteOperand(RightOperand));
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, highOperation, true, Compiler.HighByteOperand(RightOperand));
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            }
        }

        protected override int Threshold() => 8;

        protected override void Increment(int count)
        {
            IncrementOrDecrement("inx", count);
        }

        protected override void Decrement(int count)
        {
            IncrementOrDecrement("dcx", count);
        }

        private void IncrementOrDecrement(string operation, int count)
        {
            if (DestinationOperand.Register is PointerRegister pointerRegister && !Equals(RightOperand.Register, pointerRegister)) {
                pointerRegister.Load(this, LeftOperand);
                IncrementOrDecrement(this, operation, pointerRegister, count);
                return;
            }
            using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers, LeftOperand);
            var register = reservation.PointerRegister;
            register.Load(this, LeftOperand);
            IncrementOrDecrement(this, operation, register, count);
            register.Store(this, DestinationOperand);
        }

        private static void IncrementOrDecrement(Instruction instruction, string operation, Cate.PointerRegister leftRegister, int count)
        {
            Debug.Assert(count >= 0);
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + leftRegister.AsmName);
            }
            instruction.RemoveRegisterAssignment(leftRegister);
            instruction.AddChanged(leftRegister);
        }
    }
}
