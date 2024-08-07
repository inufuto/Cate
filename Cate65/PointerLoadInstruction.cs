using System.Collections.Generic;

namespace Inu.Cate.Mos6502;

internal class PointerLoadInstruction : Cate.PointerLoadInstruction
{
    public PointerLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

    public override void BuildAssembly()
    {
        if (SourceOperand.Register != null && (SourceOperand.Equals(DestinationOperand) || Equals(SourceOperand.Register, DestinationOperand.Register)))
            return;

        var candidates = new List<Cate.ByteRegister>() { ByteRegister.A, ByteRegister.X };
        using var reservation = ByteOperation.ReserveAnyRegister(this, candidates);
        var register = reservation.ByteRegister;
        register.Load(this, Compiler.LowByteOperand(SourceOperand));
        register.Store(this, Compiler.LowByteOperand(DestinationOperand));
        register.Load(this, Compiler.HighByteOperand(SourceOperand));
        register.Store(this, Compiler.HighByteOperand(DestinationOperand));
    }
}