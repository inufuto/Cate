using System.Collections.Generic;

namespace Inu.Cate.Mos6502;

internal class ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    : Cate.ByteLoadInstruction(function, destinationOperand, sourceOperand)
{
    protected override List<Cate.ByteRegister> Candidates()
    {
        var candidates = SourceOperand is IndirectOperand || DestinationOperand is IndirectOperand? [ByteRegister.A] : ByteRegister.Registers;
        return candidates;
    }
}