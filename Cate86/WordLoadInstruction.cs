﻿namespace Inu.Cate.I8086
{
    internal class WordLoadInstruction : Cate.WordLoadInstruction
    {
        public WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand)
        { }

        public override void BuildAssembly()
        {
            if (
                DestinationOperand is VariableOperand { Register: null } destinationVariableOperand
            ) {
                switch (SourceOperand) {
                    case IntegerOperand integerOperand:
                        WriteLine("\tmov word ptr [" + destinationVariableOperand.MemoryAddress() + "]," + integerOperand.IntegerValue);
                        return;
                    case PointerOperand pointerOperand:
                        WriteLine("\tmov word ptr [" + destinationVariableOperand.MemoryAddress() + "]," + pointerOperand.MemoryAddress());
                        return;
                }
            }
            if (DestinationOperand is IndirectOperand { Variable.Register: PointerRegister pointerRegister } indirectOperand) {
                var offset = indirectOperand.Offset;
                var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
                var pointer = PointerRegister.AsPointer(pointerRegister) + addition;
                switch (SourceOperand) {
                    case IntegerOperand integerOperand:
                        WriteLine("\tmov word ptr [" + pointer + "]," + integerOperand.IntegerValue);
                        return;
                    case PointerOperand pointerOperand:
                        WriteLine("\tmov word ptr [" + pointer + "]," + pointerOperand.MemoryAddress());
                        return;
                }
            }
            base.BuildAssembly();
        }
    }
}
