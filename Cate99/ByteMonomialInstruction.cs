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

            ByteOperation.UsingAnyRegister(this, DestinationOperand, SourceOperand, register =>
            {
                register.Load(this, SourceOperand);
                WriteLine("\t" + operation + "\t" + register.Name);
                register.Store(this, DestinationOperand);
            });
        }
    }
}
