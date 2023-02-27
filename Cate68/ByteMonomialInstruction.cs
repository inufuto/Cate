using System;

namespace Inu.Cate.Mc6800
{
    internal class ByteMonomialInstruction : MonomialInstruction
    {
        public ByteMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand)
            : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            var operation = OperatorId switch
            {
                '-' => "neg",
                '~' => "com",
                _ => throw new NotImplementedException()
            };

            if (DestinationOperand.SameStorage(SourceOperand)) {
                ByteOperation.Operate(this, operation, true, DestinationOperand);
                return;
            }
            using var reservation = ByteOperation.ReserveAnyRegister(this, DestinationOperand, SourceOperand);
            var register = reservation.ByteRegister;
            register.Load(this, SourceOperand);
            WriteLine("\t" + operation + register);
            register.Store(this, DestinationOperand);
        }
    }
}