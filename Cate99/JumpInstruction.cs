using System;

namespace Inu.Cate.Tms99
{
    internal class JumpInstruction : Cate.JumpInstruction
    {
        public JumpInstruction(Function function, Anchor anchor) : base(function, anchor) { }

        public override void BuildAssembly()
        {
            if (Anchor.Address != Address + 1) {
                WriteLine("\tjmp\t" + Anchor);
            }
        }
    }
}
