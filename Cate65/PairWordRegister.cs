﻿using System;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mos6502
{
    internal class PairWordRegister : Cate.WordRegister
    {
        public static PairWordRegister Xy = new(4, ByteRegister.X, ByteRegister.Y);

        private readonly ByteRegister high, low;

        public PairWordRegister(int id, ByteRegister high, ByteRegister low) : base(id, high.Name + low.Name)
        {
            this.high = high;
            this.low = low;
        }

        public override Cate.ByteRegister? High => high;
        public override Cate.ByteRegister? Low => low;


        public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            low.Save(writer, comment, instruction, tabCount);
            high.Save(writer, "", instruction, tabCount);
        }

        public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            high.Save(writer, comment, instruction, tabCount);
            low.Save(writer, "", instruction, tabCount);
        }

        //public override void Add(Instruction instruction, int offset)
        //{
        //    using var reservation = WordOperation.ReserveAnyRegister(instruction, WordZeroPage.Registers);
        //    var zeroPage = reservation.WordRegister;
        //    zeroPage.CopyFrom(instruction, this);
        //    zeroPage.Add(instruction, offset);
        //    CopyFrom(instruction, zeroPage);
        //}

        //public override bool IsOffsetInRange(int offset) => false;

        //public override bool IsPointer(int offset) => false;

        public override void LoadConstant(Instruction instruction, string value)
        {
            low.LoadConstant(instruction, "low " + value);
            high.LoadConstant(instruction, "high " + value);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            low.LoadFromMemory(instruction, label + "+0");
            high.LoadFromMemory(instruction, label + "+1");
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            low.StoreToMemory(instruction, label + "+0");
            high.StoreToMemory(instruction, label + "+1");
        }

        public override void Store(Instruction instruction, AssignableOperand operand)
        {
            low.Store(instruction, Cate.Compiler.Instance.LowByteOperand(operand));
            high.Store(instruction, Cate.Compiler.Instance.HighByteOperand(operand));
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            low.LoadFromMemory(instruction, variable, offset);
            high.LoadFromMemory(instruction, variable, offset + 1);
        }

        public override void LoadIndirect(Instruction instruction, PointerRegister pointerRegister, int offset)
        {
            low.LoadIndirect(instruction, pointerRegister, offset);
            high.LoadIndirect(instruction, pointerRegister, offset + 1);
        }

        public override void StoreIndirect(Instruction instruction, PointerRegister pointerRegister, int offset)
        {
            low.StoreIndirect(instruction, pointerRegister, offset);
            high.StoreIndirect(instruction, pointerRegister, offset + 1);
        }

        public override void CopyFrom(Instruction instruction, WordRegister sourceRegister)
        {
            if (!sourceRegister.IsPair()) return;
            Debug.Assert(sourceRegister is { Low: { }, High: { } });
            low.CopyFrom(instruction, sourceRegister.Low);
            high.CopyFrom(instruction, sourceRegister.High);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            // Must be operated in bytes
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.CopyFrom(instruction, low);
                instruction.WriteLine("\tpha");
                ByteRegister.A.CopyFrom(instruction, high);
                instruction.WriteLine("\tpha");
            }
        }

        public override void Restore(Instruction instruction)
        {
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                instruction.WriteLine("\tpla");
                high.CopyFrom(instruction, ByteRegister.A);
                instruction.WriteLine("\tpla");
                low.CopyFrom(instruction, ByteRegister.A);
            }
        }
    }
}
