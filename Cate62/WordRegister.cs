using System.Diagnostics;

namespace Inu.Cate.Sc62015
{
    internal class WordRegister : Cate.WordRegister
    {
        public static readonly List<Cate.WordRegister> Registers = new();
        public static readonly WordRegister BA = new("ba", ByteRegister.A, null);
        public static readonly WordRegister I = new("i", ByteRegister.IL, null);

        public override Cate.ByteRegister? Low { get; }
        public override Cate.ByteRegister? High { get; }
        public virtual string MV => "mv";

        public WordRegister(string name, Cate.ByteRegister? low, Cate.ByteRegister? high) : base(Compiler.NewRegisterId(), name)
        {
            Low = low;
            High = high;
            Registers.Add(this);
        }

        public override void Add(Instruction instruction, int offset)
        {
            instruction.WriteLine("\tadd " + Name + "," + offset);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override bool IsOffsetInRange(int offset)
        {
            return Math.Abs(offset) < 0x100;
        }

        public override bool IsPointer(int offset)
        {
            return IsOffsetInRange(offset);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\t" + MV + " " + Name + ", " + value);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\t" + MV + " " + Name + ",[" + label + "]");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\t" + MV + " [" + label + "]," + Name);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsOffsetInRange(offset));
            Compiler.MakePointer(instruction, pointerRegister);
            instruction.WriteLine("\t" + MV + " " + Name + "[x" + Compiler.OffsetToString(offset) + "]");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsOffsetInRange(offset));
            Compiler.MakePointer(instruction, pointerRegister);
            instruction.WriteLine("\t" + MV + " [x" + Compiler.OffsetToString(offset) + "]," + Name);
        }

        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            instruction.WriteLine("\tmv " + Name + "," + sourceRegister.Name);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (operand is ConstantOperand constantOperand) {
                instruction.WriteLine("\t" + operation + " " + Name + "," + constantOperand.MemoryAddress());
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, WordOperation.RegistersOtherThan(this), operand);
                reservation.WordRegister.Load(instruction, operand);
                instruction.WriteLine("\t" + operation + " " + Name + "," + reservation.WordRegister.Name);
            }
            if (change) {
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
        }


        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpushs " + Name + "\t" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpops " + Name + "\t" + comment);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpushs " + Name + "\t");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpops " + Name + "\t");
        }

        //public virtual List<Cate.WordRegister> OtherRegisters()
        //{
        //    return Registers.Where(r => !Equals(r, this)).ToList();
        //}
    }
}
