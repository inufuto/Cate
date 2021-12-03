using System;

namespace Inu.Cate.Mc6809
{
    internal class ByteMonomialInstruction : MonomialInstruction
    {
        public ByteMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand)
        { }

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

            ByteOperation.UsingAnyRegister(this, DestinationOperand, null, register =>
            {
                register.Load(this, SourceOperand);
                register.Operate(this, operation, true, 1);
                register.Store(this, DestinationOperand);
            });
        }
    }
}