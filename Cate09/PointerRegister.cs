using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate.Mc6809
{
    internal class PointerRegister : WordPointerRegister
    {
        public static readonly List<Cate.PointerRegister> Registers = new();
        public static readonly PointerRegister X = new PointerRegister(Mc6809.WordRegister.X);
        public static readonly PointerRegister Y = new PointerRegister(Mc6809.WordRegister.Y);
        public static readonly PointerRegister D = new PointerRegister(Mc6809.WordRegister.D);
        public static readonly List<Cate.PointerRegister> IndexRegisters = new() { X, Y };

        public PointerRegister(Cate.WordRegister wordRegister) : base(wordRegister)
        {
            Registers.Add(this);
        }


        public override bool IsOffsetInRange(int offset)
        {
            return WordRegister is IndexRegister;
        }

        public override void Add(Instruction instruction, int offset)
        {
            if (WordRegister is IndexRegister) {
                instruction.WriteLine("\tlea" + Name + "\t" + Mc6809.WordRegister.OffsetOperand(WordRegister, offset));
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
            else {
                instruction.WriteLine("\taddd\t#" + offset);
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new System.NotImplementedException();
        }
    }
}
