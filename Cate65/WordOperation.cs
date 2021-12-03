using System;
using System.Collections.Generic;

namespace Inu.Cate.Mos6502
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordZeroPage.Registers;


        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            // cannot operate
            throw new NotImplementedException();
        }

        public override Operand LowByteOperand(Operand operand)
        {
            return Compiler.LowByteOperand(operand);
        }
    }
}