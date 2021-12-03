using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mos6502
{
    internal class WordZeroPage : Cate.WordRegister
    {
        public const int MinId = 30;
        public const int Count = ByteZeroPage.Count / 2;

        public static List<Cate.WordRegister> Registers
        {
            get {
                var registers = new List<Cate.WordRegister>();
                for (var i = 0; i < Count; i++) {
                    registers.Add(new WordZeroPage(MinId + i));
                }
                return registers;
            }
        }
        public static WordRegister? FromOffset(int offset)
        {
            return new WordZeroPage(offset / 2 + MinId);
        }

        private static string IdToName(int id) => "<" + IdToLabel(id);

        public static string IdToLabel(int id) => Compiler.ZeroPageLabel + "+" + IdToOffset(id);

        private static int IdToOffset(int id)
        {
            Debug.Assert(IsIdInRange(id));
            return (id - MinId) * 2;
        }

        private static bool IsIdInRange(int id)
        {
            return id >= MinId && id < MinId + Count;
        }

        private static void UsingA(Instruction instruction, Action action)
        {
            Cate.Compiler.Instance.ByteOperation.UsingRegister(instruction, ByteRegister.A, action);
        }


        public WordZeroPage(int id) : base(id, IdToName(id))
        { }

        public int Offset => IdToOffset(Id);

        public override Cate.ByteRegister? Low => ByteZeroPage.FromOffset(Offset);
        public override Cate.ByteRegister? High => ByteZeroPage.FromOffset(Offset + 1);
        public string Label=>IdToLabel(Id);

        public static Register First = new WordZeroPage(MinId);

        public override void Add(Instruction instruction, int offset)
        {

            UsingA(instruction, () =>
            {
                Debug.Assert(Low != null && High != null);
                ByteRegister.A.CopyFrom(instruction, Low);
                ByteRegister.A.Operate(instruction, "clc|adc", true, "#low " + offset);
                Low.CopyFrom(instruction, ByteRegister.A);
                ByteRegister.A.CopyFrom(instruction, High);
                ByteRegister.A.Operate(instruction, "adc", true, "#high " + offset);
                High.CopyFrom(instruction, ByteRegister.A);
            });
            //instruction.ChangedRegisters.Add(this);
            //instruction.RemoveVariableRegister(this);
        }

        public override bool IsOffsetInRange(int offset)
        {
            return offset >= 0 && offset < 0x100;
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            Debug.Assert(Low != null && High != null);
            Low.LoadConstant(instruction, "low " + value);
            High.LoadConstant(instruction, "high " + value);
            //instruction.ChangedRegisters.Add(this);
            //instruction.RemoveVariableRegister(this);
        }

        public override bool IsPointer(int offset) => true;

        public override void Load(Instruction instruction, Operand operand)
        {
            Debug.Assert(Low != null && High != null);
            Low.Load(instruction, Cate.Compiler.Instance.LowByteOperand(operand));
            High.Load(instruction, Cate.Compiler.Instance.HighByteOperand(operand));
            //instruction.ChangedRegisters.Add(this);
            instruction.SetVariableRegister(operand, this);
        }


        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            Debug.Assert(Low != null && High != null);
            Low.LoadFromMemory(instruction, variable.MemoryAddress(offset));
            High.LoadFromMemory(instruction, variable.MemoryAddress(offset + 1));
            //instruction.ChangedRegisters.Add(this);
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            Debug.Assert(Low != null && High != null);
            Low.LoadFromMemory(instruction, label + "+0");
            High.LoadFromMemory(instruction, label + "+1");
            //instruction.ChangedRegisters.Add(this);
            //instruction.RemoveVariableRegister(this);
        }

        public override void Store(Instruction instruction, AssignableOperand operand)
        {
            Debug.Assert(Low != null && High != null);
            Low.Store(instruction, Cate.Compiler.Instance.LowByteOperand(operand));
            High.Store(instruction, Cate.Compiler.Instance.HighByteOperand(operand));
            instruction.SetVariableRegister(operand, this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            Debug.Assert(Low != null && High != null);
            Low.StoreToMemory(instruction, label + "+0");
            High.StoreToMemory(instruction, label + "+1");
        }

        public override void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
        {
            Debug.Assert(Low != null && High != null);
            Low.LoadIndirect(instruction, pointerRegister, offset);
            High.LoadIndirect(instruction, pointerRegister, offset + 1);
            //instruction.ChangedRegisters.Add(this);
            //instruction.RemoveVariableRegister(this);
        }

        public override void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
        {
            Debug.Assert(Low != null && High != null);
            Low.StoreIndirect(instruction, pointerRegister, offset);
            High.StoreIndirect(instruction, pointerRegister, offset + 1);
        }

        public override void CopyFrom(Instruction instruction, WordRegister register)
        {
            if (!(register is WordZeroPage zeroPage))
                throw new NotImplementedException();
            Debug.Assert(Low != null && High != null);
            Debug.Assert(zeroPage.Low != null && zeroPage.High != null);
            Low.CopyFrom(instruction, zeroPage.Low);
            High.CopyFrom(instruction, zeroPage.High);
            //instruction.ChangedRegisters.Add(this);
            //instruction.RemoveVariableRegister(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            // Must be operated in bytes
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            Debug.Assert(Low != null && High != null);
            Low.Save(instruction);
            High.Save(instruction);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(Low != null && High != null);
            Low.Save(writer, comment, jump, tabCount);
            High.Save(writer, comment, jump, tabCount);
        }

        public override void Restore(Instruction instruction)
        {
            Debug.Assert(Low != null && High != null);
            High.Restore(instruction);
            Low.Restore(instruction);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(Low != null && High != null);
            High.Restore(writer, comment, jump, tabCount);
            Low.Restore(writer, comment, jump, tabCount);
        }
    }
}