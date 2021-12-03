using System.Collections.Generic;

namespace Inu.Cate.Mos6502
{
    internal class ByteLoadInstruction : Cate.ByteLoadInstruction
    {
        public ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand)
        { }

        protected override List<Cate.ByteRegister> Candidates()
        {
            List<Cate.ByteRegister> candidates = SourceOperand is IndirectOperand || DestinationOperand is IndirectOperand ?
                new List<Cate.ByteRegister>() { ByteRegister.A } :
                ByteRegister.Registers;
            return candidates;
        }
    }
}