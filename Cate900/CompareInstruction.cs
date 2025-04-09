namespace Inu.Cate.Tlcs900;

internal class CompareInstruction(
    Function function,
    int operatorId,
    Operand leftOperand,
    Operand rightOperand,
    Anchor anchor)
    : Cate.CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor)
{
    public override void BuildAssembly()
    {
        if (LeftOperand is VariableOperand { Register: null } && RightOperand is ConstantOperand constantOperand) {
            var operation = "cp";
            switch (LeftOperand.Type.ByteCount) {
                case 1:
                    break;
                case 2:
                    operation += "w";
                    break;
                default:
                    throw new NotImplementedException();
            }
            ((Compiler)Cate.Compiler.Instance).OperateMemory(this, LeftOperand, operand =>
            {
                WriteLine("\t" + operation + " " + operand + "," + constantOperand.MemoryAddress());
            }, false);
            Jump();
            return;
        }
        base.BuildAssembly();
    }

    protected override void CompareByte()
    {
        using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
        var byteRegister = reservation.ByteRegister;
        byteRegister.Load(this, LeftOperand);
        if (OperatorId is Keyword.Equal or Keyword.NotEqual &&
            RightOperand is IntegerOperand { IntegerValue: 0 }) {
            WriteLine("\tor " + byteRegister + "," + byteRegister);
        }
        else {
            byteRegister.Operate(this, "cp", false, RightOperand);
        }
        Jump();
    }

    protected override void CompareWord()
    {
        using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
        var wordRegister = reservation.WordRegister;
        wordRegister.Load(this, LeftOperand);
        wordRegister.Load(this, LeftOperand);
        if (OperatorId is Keyword.Equal or Keyword.NotEqual &&
            RightOperand is IntegerOperand { IntegerValue: 0 }) {
            WriteLine("\tor " + wordRegister + "," + wordRegister);
        }
        else {
            wordRegister.Operate(this, "cp", false, RightOperand);
        }
        Jump();
    }

    private void Jump()
    {
        switch (OperatorId) {
            case Keyword.Equal:
                WriteJumpLine("\tjr eq," + Anchor);
                break;
            case Keyword.NotEqual:
                WriteJumpLine("\tjr ne," + Anchor);
                break;
            case '<':
                if (Signed) {
                    WriteJumpLine("\tjr lt," + Anchor);
                }
                else {
                    WriteJumpLine("\tjr ult," + Anchor);
                }
                break;
            case '>':
                if (Signed) {
                    WriteJumpLine("\tjr gt," + Anchor);
                }
                else {
                    WriteJumpLine("\tjr ugt," + Anchor);
                }
                break;
            case Keyword.LessEqual:
                if (Signed) {
                    WriteJumpLine("\tjr le," + Anchor);
                }
                else {
                    WriteJumpLine("\tjr ule," + Anchor);
                }
                break;
            case Keyword.GreaterEqual:
                if (Signed) {
                    WriteJumpLine("\tjr ge," + Anchor);
                }
                else {
                    WriteJumpLine("\tjr uge," + Anchor);
                }
                break;
            default:
                throw new NotImplementedException();
        }
    }
}