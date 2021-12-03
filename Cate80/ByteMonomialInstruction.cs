using System;

namespace Inu.Cate.Z80
{
    internal class ByteMonomialInstruction : MonomialInstruction
    {
        public ByteMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand sourceOperand)
            : base(function, operatorId, destinationOperand, sourceOperand)
        { }

        public override void BuildAssembly()
        {
            string operation = OperatorId switch
            {
                '-' => "neg",
                '~' => "cpl",
                _ => throw new NotImplementedException()
            };

            if (Equals(SourceOperand.Register, ByteRegister.A)) {
                WriteLine("\t" + operation);
                ByteRegister.A.Store(this, DestinationOperand);
                return;
            }

            ByteRegister.UsingAccumulator(this, () =>
            {
                ByteRegister.A.Load(this, SourceOperand);
                WriteLine("\t" + operation);
                ByteRegister.A.Store(this, DestinationOperand);
            });
        }
    }
}