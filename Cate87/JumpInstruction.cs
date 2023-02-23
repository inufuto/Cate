namespace Inu.Cate.MuCom87
{
    internal class JumpInstruction : Cate.JumpInstruction
    {
        public JumpInstruction(Function function, Anchor anchor) : base(function, anchor) { }

        public override void BuildAssembly()
        {
            if (Anchor.Address != Address + 1) {
                WriteLine("\tjr\t" + Anchor);
            }
        }
    }
}
