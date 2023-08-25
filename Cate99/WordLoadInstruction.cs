﻿namespace Inu.Cate.Tms99
{
    internal class WordLoadInstruction : Cate.WordLoadInstruction
    {
        public WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (SourceOperand.SameStorage(DestinationOperand)) return;

            var source = Tms99.Compiler.OperandToString(this, SourceOperand, false);
            var destination = Tms99.Compiler.OperandToString(this, DestinationOperand, true);
            if (SourceOperand is IntegerOperand { IntegerValue: 0 } && destination != null) {
                WriteLine("\tclr\t" + destination);
                return;
            }
            if (source != null & destination != null) {
                WriteLine("\tmov\t" + source + "," + destination);
                if (DestinationOperand.Register is WordRegister wordRegister) {
                    AddChanged(wordRegister);
                }
                return;
            }
            base.BuildAssembly();
        }
    }
}
