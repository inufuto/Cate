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
        if (RightOperand is IntegerOperand integerOperand) {
            if (integerOperand.IntegerValue == 0) {
                if (OperatorId is Keyword.Equal or Keyword.NotEqual) {
                    if (!CanOmitOperation(Flag.Z)) {
                        WriteLine("\torw\t" + wordRegister + "," + wordRegister);
                    }
                    Jump();
                    return;
                }
            }
        }
        wordRegister.Operate(this, "cmpw", false, RightOperand);
        Jump();
    }

    protected override void ComparePointer()
    {
        throw new NotImplementedException();
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