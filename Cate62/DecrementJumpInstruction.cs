﻿namespace Inu.Cate.Sc62015
{
    internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor) { }

        public override void BuildAssembly()
        {
            ByteOperation.Operate(this, "dec ", true, Operand);
            WriteJumpLine("\tjrnz " + Anchor.Label);
        }
    }
}
