using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inu.Cate.Tms99
{
    internal class PointerRegister : WordPointerRegister
    {
        private static List<Cate.PointerRegister>? registers;

        public static List<Cate.PointerRegister> Registers
        {
            get
            {
                if (registers != null) return registers;
                registers = Tms99.WordRegister.Registers.Select(r => (Cate.PointerRegister)new PointerRegister(r)).ToList();
                return registers;
            }
        }
        public static Register FromIndex(int index)
        {
            return Registers[index];
        }

        public PointerRegister(Cate.WordRegister wordRegister) : base(wordRegister) { }

        public override bool IsOffsetInRange(int offset) => Index != 0 || offset == 0;
        public int Index
        {
            get
            {
                Debug.Assert(WordRegister != null);
                return ((Tms99.WordRegister)WordRegister).Index;
            }
        }

        public override void Add(Instruction instruction, int offset)
        {
            instruction.WriteLine("\tai\t" + Name + "," + offset);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new NotImplementedException();
        }
    }
}
