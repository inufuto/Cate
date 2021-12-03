using System;

namespace Inu.Cate.Mc6800
{
    internal class ByteMonomialInstruction : MonomialInstruction
    {
        public ByteMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand)
            : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            string operation = OperatorId switch
            {
                '-' => "neg",
                '~' => "com",
                _ => throw new NotImplementedException()
            };

            if (DestinationOperand.SameStorage(SourceOperand)) {
                ByteOperation.Operate(this, operation, true, DestinationOperand);
                return;
            }

            ByteRegister.UsingAny(this, DestinationOperand, register =>
            {
                register.Load(this, SourceOperand);
                WriteLine("\t" + operation + register);
                register.Store(this,  DestinationOperand);
            });
        }
    }
}