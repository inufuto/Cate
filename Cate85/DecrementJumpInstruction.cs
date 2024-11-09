namespace Inu.Cate.Sm85;

internal class DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
    : Cate.DecrementJumpInstruction(function, operand, anchor)
{
    public override void BuildAssembly()
    {
        if (Operand.Register is ByteRegister byteRegister) {
            WriteLine("\tdbnz\t" + byteRegister+","+Anchor.Label);
            return;
        }
        ByteOperation.Operate(this, "dec\t", true, Operand);
        WriteJumpLine("\tbr\tnz," + Anchor.Label);
    }
}