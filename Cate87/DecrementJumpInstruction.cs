namespace Inu.Cate.MuCom87
{
    internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor) { }

        public override void BuildAssembly()
        {
            var registerInUse = IsRegisterInUse(ByteRegister.A);
            if (registerInUse) {
                WriteLine("\tstaw\t" + MuCom87.Compiler.TemporaryByte);
            }
            ByteRegister.A.Load(this, Operand);
            WriteLine("\tsui\ta,1");
            ByteRegister.A.Store(this, Operand);
            if (registerInUse) {
                WriteLine("\tldaw\t" + MuCom87.Compiler.TemporaryByte);
            }

            ((MuCom87.Compiler)Compiler).SkipIfZero(this);
            WriteJumpLine("\tjr\t" + Anchor.Label);
        }
    }
}
