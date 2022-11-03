namespace Inu.Cate.I8080
{
    internal class JumpInstruction : Inu.Cate.JumpInstruction
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
