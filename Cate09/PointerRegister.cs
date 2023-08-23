using System.Collections.Generic;

namespace Inu.Cate.Mc6809
{
    internal class PointerRegister : WordPointerRegister
    {
        public static readonly List<Cate.PointerRegister> Registers = new();
        public static readonly PointerRegister X = new(Mc6809.WordRegister.X);
        public static readonly PointerRegister Y = new(Mc6809.WordRegister.Y);
        public static readonly PointerRegister D = new(Mc6809.WordRegister.D);
        public static readonly List<Cate.PointerRegister> IndexRegisters = new() { X, Y };

        public PointerRegister(Cate.WordRegister wordRegister) : base(2, wordRegister)
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
            var register = operand.Register;
            if (register is PointerRegister rightRegister) {
                instruction.WriteLine("\tst" + rightRegister + "\t" + DirectPage.Word);
                instruction.WriteLine("\t" + operation + Name + "\t" + DirectPage.Word);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            }
            Mc6809.WordOperation.Operate(instruction, operation + Name, change, operand, 1);
        }

        public override void Exchange(Instruction instruction, Cate.PointerRegister register)
        {
            instruction.WriteLine("\texg\t" + register + "," + this);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(register);
            instruction.AddChanged(register);
        }
    }
}
