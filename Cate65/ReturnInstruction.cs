using System.Linq;

namespace Inu.Cate.Mos6502
{
    class ReturnInstruction : Cate.ReturnInstruction
    {
        public ReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor) : base(function, sourceOperand, anchor)
        { }

        public override void BuildAssembly()
        {
            LoadResult();
            //if (SourceOperand != null) {
            //    if (SourceOperand.Type.ByteCount == 1) {
            //        Compiler.OperateByte(this, SourceOperand, (sourceOffset, source) =>
            //        {
            //            if (sourceOffset != null) {
            //                Compiler.WriteYOffset(this, sourceOffset);
            //                WriteLine("\tlda\t" + source);
            //                WriteLine("\ttay");
            //            }
            //            else {
            //                WriteLine("\tldy\t" + source);
            //            }
            //        });
            //    }
            //    else {
            //        Compiler.OperateWord(this, SourceOperand,
            //            (sourceLowOffset, sourceLow, sourceHighOffset, sourceHigh) =>
            //            {
            //                if (sourceHighOffset != null) {
            //                    Compiler.WriteYOffset(this, sourceHighOffset);
            //                    WriteLine("\tlda\t" + sourceHigh);
            //                    WriteLine("\ttax");
            //                }
            //                else {
            //                    WriteLine("\tldx\t" + sourceHigh);
            //                }
            //                if (sourceLowOffset != null) {
            //                    Compiler.WriteYOffset(this, sourceLowOffset);
            //                    WriteLine("\tlda\t" + sourceLow);
            //                    WriteLine("\ttay");
            //                }
            //                else {
            //                    WriteLine("\tldy\t" + sourceLow);
            //                }
            //            });
            //    }
            //}
            if (!Equals(Function.Instructions.Last())) {
                WriteLine("\tjmp\t" + Anchor.Label);
            }
        }
    }
}