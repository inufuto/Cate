using System.Diagnostics;

namespace Inu.Cate.Sc62015
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public static readonly Accumulator A = new("a");
        public static readonly ByteRegister IL = new("il");
        public static List<Cate.ByteRegister> Registers = new() { A, IL };

        public static List<Cate.ByteRegister> AccumulatorAndInternalRam
        {
            get
            {
                var registers = new List<Cate.ByteRegister> { ByteRegister.A };
                registers.AddRange(ByteInternalRam.Registers);
                return registers;
            }
        }

        public override Cate.WordRegister? PairRegister => WordRegister.Registers.FirstOrDefault(r => Equals(r.Low, this));

        public override bool Conflicts(Register? register)
        {
            return Equals(register, PairRegister) || base.Conflicts(register);
        }


        protected ByteRegister(string name) : base(Compiler.NewRegisterId(), name) { }

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

        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            Debug.Assert(pointerRegister.IsOffsetInRange(offset));
            instruction.WriteLine("\tmv " + AsmName + ",[" + pointerRegister.AsmName + Compiler.OffsetToString(offset) + "]");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            if (pointerRegister.IsOffsetInRange(offset)) {
                instruction.WriteLine("\tmv [" + pointerRegister.AsmName + Compiler.OffsetToString(offset) + "]," + AsmName);
            }
            else {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    StoreIndirect(instruction, pointerRegister, 0);
                });
            }
        }


        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            instruction.WriteLine("\tmv " + AsmName + "," + sourceRegister.AsmName);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + " " + AsmName);
            }
            if (change) {
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    instruction.WriteLine("\t" + operation + " " + AsmName + "," + integerOperand.IntegerValue);
                    instruction.RemoveRegisterAssignment(this);
                    break;
                case VariableOperand variableOperand: {
                        var variableRegister = instruction.GetVariableRegister(variableOperand);
                        if (variableRegister is ByteRegister) {
                            instruction.WriteLine("\t" + operation + "," + variableRegister.AsmName);
                        }
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            throw new NotImplementedException();
        }
    }
}
