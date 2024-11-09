namespace Inu.Cate.Sc62015
{
    internal class WordRegister : Cate.WordRegister
    {
        public static readonly WordRegister BA = new("ba", ByteRegister.A, null);
        public static readonly WordRegister I = new("i", ByteRegister.IL, null);
        public static readonly List<Cate.WordRegister> Registers = new() { BA, I };

        public override Cate.ByteRegister? Low { get; }
        public override Cate.ByteRegister? High { get; }
        public virtual string MV => "mv";

        public WordRegister(string name, Cate.ByteRegister? low, Cate.ByteRegister? high) : base(Compiler.NewRegisterId(), name)
        {
            Low = low;
            High = high;
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\t" + MV + " " + AsmName + "," + value);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\t" + MV + " " + AsmName + ",[" + label + "]");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\t" + MV + " [" + label + "]," + AsmName);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister wordRegister, int offset)
        {
            if (wordRegister is PointerRegister pointerRegister) {
                LoadIndirect(instruction, pointerRegister, offset);
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, PointerRegister.Registers);
                reservation.WordRegister.CopyFrom(instruction, wordRegister);
                LoadIndirect(instruction, (PointerRegister)reservation.WordRegister, offset);
            }
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister wordRegister, int offset)
        {
            if (wordRegister is PointerRegister pointerRegister) {
                StoreIndirect(instruction, pointerRegister, offset);
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, PointerRegister.Registers);
                reservation.WordRegister.CopyFrom(instruction, wordRegister);
                StoreIndirect(instruction, (PointerRegister)reservation.WordRegister, offset);
            }
        }

        private void LoadIndirect(Instruction instruction, PointerRegister pointerRegister, int offset)
        {
            if (pointerRegister.IsOffsetInRange(offset)) {
                instruction.WriteLine("\t" + MV + " " + AsmName + ",[" + pointerRegister.AsmName + Compiler.OffsetToString(offset) + "]");
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
            else {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    LoadIndirect(instruction, pointerRegister, 0);
                });
            }
        }

        private void StoreIndirect(Instruction instruction, PointerRegister pointerRegister, int offset)
        {
            if (pointerRegister.IsOffsetInRange(offset)) {
                instruction.WriteLine("\t" + MV + " [" + pointerRegister.AsmName + Compiler.OffsetToString(offset) +
                                      "]," + AsmName);
            }
            else {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    StoreIndirect(instruction, pointerRegister, 0);
                });
            }
        }

        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            var mv = sourceRegister is not WordInternalRam ? "mv" : MV;
            instruction.WriteLine("\t" + mv + " " + AsmName + "," + sourceRegister.AsmName);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (operand is ConstantOperand constantOperand) {
                instruction.WriteLine("\t" + operation + " " + AsmName + "," + constantOperand.MemoryAddress());
            }
            else {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers, operand);
                reservation.WordRegister.Load(instruction, operand);
                instruction.WriteLine("\t" + operation + " " + AsmName + "," + reservation.WordRegister.AsmName);
            }
            if (change) {
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
        }

        public override bool IsOffsetInRange(int offset) => false;

        public override void Add(Instruction instruction, int offset)
        {
            throw new NotImplementedException();
        }


        public override bool Contains(Cate.ByteRegister byteRegister)
        {
            return Equals(byteRegister, this.Low) || base.Contains(byteRegister);
        }

        public override bool Conflicts(Register? register)
        {
            return Equals(register, this.Low) || base.Conflicts(register);
        }

        public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpushs " + AsmName + "\t" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpops " + AsmName + "\t" + comment);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpushs " + AsmName + "\t");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpops " + AsmName + "\t");
        }
    }
}
