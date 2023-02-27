namespace Inu.Cate.MuCom87
{
    internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
    {
        public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor) { }

        public override void BuildAssembly()
        {
            var registerReserved = IsRegisterReserved(ByteRegister.A);
            if (registerReserved) {
                WriteLine("\tstaw\t" + MuCom87.Compiler.TemporaryByte);
            }
            ByteRegister.A.Load(this, Operand);
            WriteLine("\tsui\ta,1");
            ByteRegister.A.Store(this, Operand);
            if (registerReserved) {
                WriteLine("\tldaw\t" + MuCom87.Compiler.TemporaryByte);
            }

            ((MuCom87.Compiler)Compiler).SkipIfZero(this);
            WriteJumpLine("\tjr\t" + Anchor.Label);
        }
    }
}
