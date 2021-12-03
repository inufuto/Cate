using System.Collections.Generic;

namespace Inu.Cate.Z80
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            // cannot operate
            throw new System.NotImplementedException();
        }

        public override Operand LowByteOperand(Operand operand)
        {
            return Compiler.LowByteOperand(operand);
        }
    }
}
