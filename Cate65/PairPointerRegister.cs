using System;
using System.Diagnostics;

namespace Inu.Cate.Mos6502
{
    internal class PairPointerRegister : Cate.WordPointerRegister
    {
        public static PairPointerRegister Xy = new PairPointerRegister(PairWordRegister.Xy);

        public PairPointerRegister(WordRegister wordRegister) : base(wordRegister)
        { }

        //public override bool IsAddable() => true;

        public override bool IsOffsetInRange(int offset)
        {
            return offset is >= 0 and < 0x100;
        }

        public override void Add(Instruction instruction, int offset)
        {
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                Debug.Assert(Low != null && High != null);
                ByteRegister.A.CopyFrom(instruction, Low);
                ByteRegister.A.Operate(instruction, "clc|adc", true, "#low " + offset);
                Low.CopyFrom(instruction, ByteRegister.A);
                ByteRegister.A.CopyFrom(instruction, High);
                ByteRegister.A.Operate(instruction, "adc", true, "#high " + offset);
                High.CopyFrom(instruction, ByteRegister.A);
            }
        }
    }
}
