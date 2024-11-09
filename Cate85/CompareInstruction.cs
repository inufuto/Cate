using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class CompareInstruction(
    Function function,
    int operatorId,
    Operand leftOperand,
    Operand rightOperand,
    Anchor anchor)
    : Cate.CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor)
{
    protected override void CompareByte()
    {
        var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
        var byteRegister = reservation.ByteRegister;
        byteRegister.Load(this, LeftOperand);
        if (RightOperand is IntegerOperand integerOperand) {
            if (integerOperand.IntegerValue == 0) {
                if (OperatorId is Keyword.Equal or Keyword.NotEqual) {
                    if (!CanOmitOperation(Flag.Z)) {
                        WriteLine("\tor\t" + byteRegister + "," + byteRegister);
                    }
                    Jump();
                    return;
                }
            }
        }
        byteRegister.Operate(this, "cmp", false, RightOperand);
        Jump();
    }

    protected override void CompareWord()
    {
        var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
        var wordRegister = reservation.WordRegister;
        wordRegister.Load(this, LeftOperand);
        if (CompareWord(wordRegister)) return;
        Jump();
    }

    private bool CompareWord(Cate.WordRegister leftRegister)
    {
        if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
            if (OperatorId is Keyword.Equal or Keyword.NotEqual) {
                if (!CanOmitOperation(Flag.Z)) {
                    WriteLine("\torw\t" + leftRegister + "," + leftRegister);
                }
                Jump();
                return true;
            }
        }
        leftRegister.Operate(this, "cmpw", false, RightOperand);
        return false;
    }

    protected override void ComparePointer()
    {
        var reservation = PointerOperation.ReserveAnyRegister(this, LeftOperand);
        var pointerRegister = reservation.PointerRegister;
        pointerRegister.Load(this, LeftOperand);
        Debug.Assert(pointerRegister.WordRegister != null);
        if (CompareWord(pointerRegister.WordRegister)) return;
        Jump();
    }

    private void Jump()
    {
        switch (OperatorId) {
            case Keyword.Equal:
                WriteJumpLine("\tbr\teq," + Anchor);
                break;
            case Keyword.NotEqual:
                WriteJumpLine("\tbr\tne," + Anchor);
                break;
            case '<':
                if (Signed) {
                    WriteJumpLine("\tbr\tlt," + Anchor);
                }
                else {
                    WriteJumpLine("\tbr\tult," + Anchor);
                }

                break;
            case '>':
                if (Signed) {
                    WriteJumpLine("\tbr\tgt," + Anchor);
                }
                else {
                    WriteJumpLine("\tbr\tugt," + Anchor);
                }

                break;
            case Keyword.LessEqual:
                if (Signed) {
                    WriteJumpLine("\tbr\tle," + Anchor);
                }
                else {
                    WriteJumpLine("\tbr\tule," + Anchor);
                }

                break;
            case Keyword.GreaterEqual:
                if (Signed) {
                    WriteJumpLine("\tbr\tge," + Anchor);
                }
                else {
                    WriteJumpLine("\tbr\tuge," + Anchor);
                }

                break;
            default:
                throw new NotImplementedException();
        }
    }

}