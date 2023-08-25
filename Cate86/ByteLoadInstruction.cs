﻿namespace Inu.Cate.I8086
{
    internal class ByteLoadInstruction : Cate.ByteLoadInstruction
    {
        public ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand)
        { }

        public override void BuildAssembly()
        {
            if (
                DestinationOperand is VariableOperand { Register: null } destinationVariableOperand
            ) {
                if (SourceOperand is IntegerOperand integerOperand) {
                    WriteLine("\tmov byte ptr [" + destinationVariableOperand.MemoryAddress() + "]," + integerOperand.IntegerValue);
                    return;
                }
            }
            if (DestinationOperand is IndirectOperand { Variable: { Register: PointerRegister pointerRegister } } destinationIndirectOperand) {
                if (SourceOperand is IntegerOperand integerOperand) {
                    var offset = destinationIndirectOperand.Offset;
                    var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
                    WriteLine("\tmov byte ptr [" + PointerRegister.AsPointer(pointerRegister) + addition + "]," + integerOperand.IntegerValue);
                    return;
                }
            }
            base.BuildAssembly();
        }
    }
}
