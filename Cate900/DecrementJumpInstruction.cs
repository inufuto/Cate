namespace Inu.Cate.Tlcs900;

internal class DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
    : Cate.DecrementJumpInstruction(function, operand, anchor)
{
    public override void BuildAssembly()
    {
        if (Operand.Register is ByteRegister byteRegister) {
            WriteLine("\tdjnz " + byteRegister + "," + Anchor.Label);
            AddChanged(byteRegister);
            RemoveRegisterAssignment(byteRegister);
            return;
        }
        if (Operand.Register is WordRegister wordRegister) {
            WriteLine("\tdjnz " + wordRegister + "," + Anchor.Label);
            AddChanged(wordRegister);
            RemoveRegisterAssignment(wordRegister);
            return;
        }
        ((Compiler)Cate.Compiler.Instance).OperateMemory(this, Operand, operand =>
        {
            WriteLine("\tdec 1," + operand);
        });
        WriteJumpLine("\tjr\tnz," + Anchor.Label);
    }
}