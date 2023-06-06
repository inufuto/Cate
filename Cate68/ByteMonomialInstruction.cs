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

            {
                void ViaRegister(Cate.ByteRegister r)
                {
                    r.Load(this, SourceOperand);
                    WriteLine("\t" + operation + r);
                }

                if (DestinationOperand.Register is ByteRegister byteRegister) {
                    ViaRegister(byteRegister);
                    return;
                }
                using var reservation = ByteOperation.ReserveAnyRegister(this, SourceOperand);
                var register = reservation.ByteRegister;
                ViaRegister(register);
                register.Store(this, DestinationOperand);
            }
        }
    }
}