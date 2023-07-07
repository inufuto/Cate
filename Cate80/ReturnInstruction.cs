﻿using System.Linq;

namespace Inu.Cate.Z80
{
    internal class ReturnInstruction : Cate.ReturnInstruction
    {
        public ReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor) : base(function, sourceOperand, anchor) { }

        public override void BuildAssembly()
        {
            LoadResult();
            if (!Equals(Function.Instructions.Last())) {
                WriteLine("\tjr\t" + Anchor.Label);
            }
        }
    }
}