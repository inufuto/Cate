namespace Inu.Cate.Mos6502
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType)
        { }

        //protected override void Reduce()
        //{
        //    if (SourceOperand.Equals(DestinationOperand)) return;
        //    Compiler.OperateWord(this, SourceOperand, (sourceLowOffset, sourceLow, sourceHighOffset, sourceHigh) =>
        //    {
        //        Compiler.OperateByte(this, DestinationOperand, (destinationOffset, destination) =>
        //        {
        //            Compiler.WriteYOffset(this, sourceLowOffset);
        //            WriteLine("\tlda\t" + sourceLow);
        //            Compiler.WriteYOffset(this, destinationOffset);
        //            WriteLine("\tsta\t" + destination);
        //        });
        //    });
        //}

        protected override void ExpandSigned()
        {
            ByteOperation.UsingRegister(this, ByteRegister.A,  () =>
            {
                ByteRegister.A.Load(this, SourceOperand);
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                ByteRegister.A.Operate(this, "asl", true, 1);
                ByteOperation.UsingAnyRegister(this, ByteZeroPage.Registers, temporary =>
                {
                    temporary.CopyFrom(this, ByteRegister.A);
                    ByteRegister.A.Operate(this, "sbc", true, temporary.Name);
                });
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            });
        }

        //protected override void Expand()
        //{
        //    Compiler.OperateByte(this, SourceOperand, (sourceOffset, source) =>
        //    {
        //        Compiler.OperateWord(this, DestinationOperand,
        //            (destinationLowOffset, destinationLow, destinationHighOffset, destinationHigh) =>
        //            {
        //                Compiler.WriteYOffset(this, sourceOffset);
        //                WriteLine("\tlda\t" + source);
        //                Compiler.WriteYOffset(this, destinationLowOffset);
        //                WriteLine("\tsta\t" + destinationLow);
        //                WriteLine("\tlda\t#0");
        //                Compiler.WriteYOffset(this, destinationHighOffset);
        //                WriteLine("\tsta\t" + destinationHigh);
        //            });
        //    });
        //}
    }
}