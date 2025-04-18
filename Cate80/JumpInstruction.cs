﻿namespace Inu.Cate.Z80;

internal class JumpInstruction(Function function, Anchor anchor) : Inu.Cate.JumpInstruction(function, anchor)
{
    public override void BuildAssembly()
    {
        if (Anchor.Address != Address + 1) {
            WriteLine("\tjr\t" + Anchor);
        }
    }
}