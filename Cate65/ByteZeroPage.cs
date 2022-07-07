using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mos6502
{
    internal class ByteZeroPage : Cate.ByteRegister
    {
        public const int MinId = 10;
        public const int Count = 16 - 2;
        public static string TemporaryByte = "<" + Compiler.ZeroPageLabel + "+" + Count;

        public static List<Cate.ByteRegister> Registers
        {
            get {
                var registers = new List<Cate.ByteRegister>();
                for (var i = 0; i < Count; i++) {
                    registers.Add(new ByteZeroPage(MinId + i));
                }
                return registers;
            }
        }


        public static ByteZeroPage FromOffset(int offset)
        {
            return new ByteZeroPage(offset + MinId);
        }

        private static string IdToName(int id)
        {
            var offset = IdToOffset(id);
            return "<" + Compiler.ZeroPageLabel + "+" + offset.ToString();
        }

        private static int IdToOffset(int id)
        {
            Debug.Assert(IsIdInRange(id));
            return id - MinId;
        }

        private static bool IsIdInRange(int id)
        {
            return id >= MinId && id < MinId + Count;
        }


        public ByteZeroPage(int id) : base(id, IdToName(id)) { }

        public int Offset => IdToOffset(Id);

        public override WordRegister? PairRegister => WordZeroPage.FromOffset(Offset);
        public static Register First => new ByteZeroPage(MinId);

        public override void LoadConstant(Instruction instruction, string value)
        {
            ByteOperation.UsingAnyRegister(instruction, ByteRegister.Registers, register =>
            {
                register.LoadConstant(instruction, value);
                register.StoreToMemory(instruction, Name);
            });
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            ByteOperation.UsingAnyRegister(instruction, ByteRegister.Registers, register =>
            {
                register.LoadFromMemory(instruction, variable, offset);
                register.StoreToMemory(instruction, Name);
                instruction.ChangedRegisters.Add(this);
            });
            instruction.ChangedRegisters.Add(this);
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            ByteOperation.UsingAnyRegister(instruction, ByteRegister.Registers, register =>
            {
                register.LoadFromMemory(instruction, Name);
                register.StoreToMemory(instruction, variable, offset);
            });
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void LoadIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
        {
            var candidates = new List<Cate.ByteRegister>() { ByteRegister.X, ByteRegister.A };
            ByteOperation.UsingAnyRegister(instruction, candidates, register =>
            {
                register.LoadIndirect(instruction, pointerRegister, offset);
                register.StoreToMemory(instruction, Name);
            });
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreIndirect(Instruction instruction, WordRegister pointerRegister, int offset)
        {
            var candidates = new List<Cate.ByteRegister>() { ByteRegister.X, ByteRegister.A };
            ByteOperation.UsingAnyRegister(instruction, candidates, register =>
             {
                 register.LoadFromMemory(instruction, Name);
                 register.StoreIndirect(instruction, pointerRegister, offset);
             });
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            ByteOperation.UsingAnyRegister(instruction, ByteRegister.Registers, register =>
            {
                register.LoadFromMemory(instruction, label);
                register.StoreToMemory(instruction, Name);
            });
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            ByteOperation.UsingAnyRegister(instruction, ByteRegister.Registers, register =>
            {
                register.LoadFromMemory(instruction, Name);
                register.StoreToMemory(instruction, label);
            });
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister register)
        {
            if (register is ByteRegister byteRegister) {
                byteRegister.StoreToMemory(instruction, Name);
            }
            else {
                ByteOperation.UsingAnyRegister(instruction, ByteRegister.Registers, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, register);
                    temporaryRegister.StoreToMemory(instruction, Name);
                });
            }
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + Name);
            }
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.LoadFromMemory(instruction, Name);
                ByteRegister.A.Operate(instruction, operation, change, operand);
                ByteRegister.A.StoreToMemory(instruction, Name);
            });
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.LoadFromMemory(instruction, Name);
                ByteRegister.A.Operate(instruction, operation, change, operand);
                ByteRegister.A.StoreToMemory(instruction, Name);
            });
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Save(Instruction instruction)
        {
            Cate.Compiler.Instance.ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.CopyFrom(instruction, this);
                ByteRegister.A.Save(instruction);
            });
        }

        public override void Restore(Instruction instruction)
        {
            Cate.Compiler.Instance.ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.Restore(instruction);
                CopyFrom(instruction, ByteRegister.A);
            });
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tlda\t" + this + comment);
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpha");
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpla");
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tsta\t" + this + comment);
        }
    }
}