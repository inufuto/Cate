using System.Collections.Generic;
using System.IO;

namespace Inu.Cate.MuCom87
{
    internal class ByteWorkingRegister : Cate.ByteRegister
    {
        public const string WorkingRegisterLabel = "@Working";
        public const int MinId = 20;
        public const int Count = 0;

        private static int IdToOffset(int id)
        {
            return id - MinId;
        }

        private static string IdToName(int id) => WorkingRegisterLabel + "+" + IdToOffset(id);

        public static List<Cate.ByteRegister> Registers
        {
            get
            {
                var registers = new List<Cate.ByteRegister>();
                for (var i = 0; i < Count; i++) {
                    registers.Add(new ByteWorkingRegister(MinId + i));
                }
                return registers;
            }
        }


        public ByteWorkingRegister(int id) : base(id, IdToName(id))
        { }


        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tldaw\t" + Name + comment);
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpush\tv");
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpop\tv");
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tstaw\t" + Name + comment);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tmviw\t" + Name + "," + value);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.LoadFromMemory(instruction, variable, offset);
                CopyFrom(instruction, ByteRegister.A);
            });
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.CopyFrom(instruction, this);
                ByteRegister.A.StoreToMemory(instruction, variable, offset);
            });
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.LoadIndirect(instruction, pointerRegister, offset);
                CopyFrom(instruction, ByteRegister.A);
            });
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.CopyFrom(instruction, this);
                ByteRegister.A.StoreIndirect(instruction, pointerRegister, offset);
            });
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.LoadFromMemory(instruction, label);
                CopyFrom(instruction, ByteRegister.A);
            });
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.CopyFrom(instruction, this);
                ByteRegister.A.StoreToMemory(instruction, label);
            });
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            if (sourceRegister is Accumulator accumulator) {
                instruction.WriteLine("\tstaw\t" + Name);
                return;
            }
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.CopyFrom(instruction, sourceRegister);
                CopyFrom(instruction, ByteRegister.A);
            });
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.CopyFrom(instruction, this);
                ByteRegister.A.Operate(instruction, operation, change, count);
                if (change) {
                    CopyFrom(instruction, ByteRegister.A);
                }
            });
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.CopyFrom(instruction, this);
                ByteRegister.A.Operate(instruction, operation, change, operand);
                if (change) {
                    CopyFrom(instruction, ByteRegister.A);
                }
            });
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.CopyFrom(instruction, this);
                ByteRegister.A.Operate(instruction, operation, change, operand);
                if (change) {
                    CopyFrom(instruction, ByteRegister.A);
                }
            });
        }

        public override void Save(Instruction instruction)
        {
            var registerInUse = instruction.IsRegisterInUse(ByteRegister.A);
            if (registerInUse) {
                instruction.WriteLine("\tstaw\t" + Compiler.TemporaryByte);
            }
            ByteRegister.A.CopyFrom(instruction, this);
            ByteRegister.A.Save(instruction);
            if (registerInUse) {
                instruction.WriteLine("\tldaw\t" + Compiler.TemporaryByte);
            }
        }

        public override void Restore(Instruction instruction)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.Restore(instruction);
                CopyFrom(instruction, ByteRegister.A);
            });
        }

        public static Cate.ByteRegister FromOffset(int offset)
        {
            return new ByteWorkingRegister(offset + MinId);
        }

    }
}
