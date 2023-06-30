using System.Diagnostics;
using System.Reflection.Emit;

namespace Inu.Cate.Sc62015
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public static List<Cate.ByteRegister> Registers = new();
        public static readonly Accumulator A = new("a", WordRegister.BA);
        public static readonly ByteRegister IL = new("il", WordRegister.I);

        public override Cate.WordRegister? PairRegister { get; }


        protected ByteRegister(string name, WordRegister? wordRegister) : base(Compiler.NewRegisterId(), name)
        {
            PairRegister = wordRegister;
            Registers.Add(this);
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

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tmv " + Name + "," + value);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmv " + Name + ",[" + label + "]");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            LoadFromMemory(instruction, variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmv [" + label + "]," + Name);
        }


        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            StoreToMemory(instruction, variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsOffsetInRange(offset));
            Compiler.MakePointer(instruction, pointerRegister);
            instruction.WriteLine("\tmv " + Name + "[x" + Compiler.OffsetToString(offset) + "]");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsOffsetInRange(offset));
            Compiler.MakePointer(instruction, pointerRegister);
            instruction.WriteLine("\tmv [x" + Compiler.OffsetToString(offset) + "]," + Name);
        }


        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            instruction.WriteLine("\tmv " + Name + "," + sourceRegister.Name);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + Name);
            }
            if (change) {
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new NotImplementedException();
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            throw new NotImplementedException();
        }
    }
}
