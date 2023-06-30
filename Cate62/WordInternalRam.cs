using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Inu.Cate.Sc62015
{
    internal class WordInternalRam : WordRegister
    {
        public const int Count = 8;
        public static readonly WordInternalRam BX = new WordInternalRam("bx", "0xd4", ByteInternalRam.BL, ByteInternalRam.BL);
        public static readonly WordInternalRam CX = new WordInternalRam("cx", "0xd6", ByteInternalRam.CL, ByteInternalRam.CH);
        public static readonly WordInternalRam DX = new WordInternalRam("dx", "0xd8", ByteInternalRam.DL, ByteInternalRam.DH);

        public new static List<Cate.WordRegister> Registers
        {
            get
            {
                var registers = new List<Cate.WordRegister>() { BX, CX, DX };
                for (var index = 0; index < Count; index++) {
                    registers.Add(new WordInternalRam(index));
                }
                return registers;
            }
        }

        public static List<Cate.WordRegister> RegistersOtherThan(WordInternalRam register)
        {
            return Registers.Where(r => !Equals(r, register)).Select(r => (Cate.WordRegister)r).ToList();
        }


        private static string IndexToName(int index)
        {
            return "(" + IndexToLabel(index) + ")";
        }

        private static string IndexToLabel(int index)
        {
            return ("<@w" + index);
        }

        public override Cate.ByteRegister? Low { get; }
        public override Cate.ByteRegister? High { get; }
        public override string MV => "mvw";

        public override void Add(Instruction instruction, int offset)
        {
            using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers);
            reservation.WordRegister.CopyFrom(instruction, this);
            reservation.WordRegister.Add(instruction, offset);
            CopyFrom(instruction, reservation.WordRegister);
        }

        public override bool IsOffsetInRange(int offset)
        {
            throw new NotImplementedException();
        }

        public override bool IsPointer(int offset)
        {
            throw new NotImplementedException();
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tmvw " + Name + "," + value);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmvw " + Name + ",[" + label + "]");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmvw [" + label + "]," + Name);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsOffsetInRange(offset));
            instruction.WriteLine("\tmvw " + Name + "[" + pointerRegister.Name + Compiler.OffsetToString(offset) + "]");
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsOffsetInRange(offset));
            instruction.WriteLine("\tmvw [" + pointerRegister.Name + Compiler.OffsetToString(offset) + "]," + Name);
        }

        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            switch (sourceRegister) {
                case WordInternalRam:
                    instruction.WriteLine("\tmvw " + Name + "," + sourceRegister.Name);
                    break;
                case WordRegister:
                    instruction.WriteLine("\tmv " + Name + "," + sourceRegister.Name);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new NotImplementedException();
        }

        public readonly string Label;

        public WordInternalRam(int index) : this(IndexToName(index), IndexToLabel(index), null, null) { }

        private WordInternalRam(string name, string label, Cate.ByteRegister? low, Cate.ByteRegister? high) : base(name,low,high)
        {
            Label = label;
            Low = low;
            High = high;
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            throw new NotImplementedException();
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            throw new NotImplementedException();
        }

        public override void Restore(Instruction instruction)
        {
            throw new NotImplementedException();
        }

    }
}
