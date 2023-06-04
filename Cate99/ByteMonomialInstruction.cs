using System;

namespace Inu.Cate.Tms99
{
    internal class ByteMonomialInstruction : MonomialInstruction
    {
        public ByteMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            var operation = OperatorId switch
            {
                '-' => "neg",
                '~' => "inv",
                _ => throw new NotImplementedException()
            };

            void ForRegister(Cate.ByteRegister byteRegister)
            {
                string s;
                byteRegister.Load(this, SourceOperand);
                WriteLine("\t" + operation + "\t" + byteRegister.Name);
            }

            if (DestinationOperand.Register is ByteRegister byteRegister) {
                ForRegister(byteRegister);
                return;
            }
            using var reservation = ByteOperation.ReserveAnyRegister(this, DestinationOperand, SourceOperand);
            var register = reservation.ByteRegister;
            ForRegister(register);
            register.Store(this, DestinationOperand);
        }
    }
}
