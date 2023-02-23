using System;
using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.MuCom87
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;
        //public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        //{
        //    // cannot operate
        //    throw new NotImplementedException();
        //}

        //public override Operand LowByteOperand(Operand operand)
        //{
        //    return Compiler.LowByteOperand(operand);
        //}

        protected override bool CanCopyRegisterToSave(Instruction instruction, Cate.WordRegister register) => false;
    }
}
