namespace Inu.Cate.Hd61700;

internal class CompareInstruction : Cate.CompareInstruction
{
    private static int subLabelIndex;

    public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor) { }

    private Register? LeftRegister()
    {
        if (LeftOperand is VariableOperand variableOperand) {
            return GetVariableRegister(variableOperand);
        }
        return null;
    }

    protected override void CompareByte()
    {
        Compare(() =>
        {
            using var lrr = ByteOperation.ReserveAnyRegister(this, LeftOperand);
            var leftRegister = lrr.ByteRegister;
            leftRegister.Load(this, LeftOperand);
            if (RightOperand is IntegerOperand integerOperand) {
                WriteLine("\tsbc " + leftRegister + "," + ByteRegister.IntValue(integerOperand.IntegerValue));
            }
            else {
                using var rrr = ByteOperation.ReserveAnyRegister(this, RightOperand);
                var rightRegister = rrr.ByteRegister;
                rightRegister.Load(this, RightOperand);
                WriteLine("\tsbc " + leftRegister + "," + rightRegister);
            }
        }, () =>
        {
            var leftRegister = ByteRegister.Registers[0];
            using (ByteOperation.ReserveRegister(this, leftRegister)) {
                leftRegister.Load(this, RightOperand);
                var rightRegister = ByteRegister.Registers[1];
                using (ByteOperation.ReserveRegister(this, rightRegister)) {
                    rightRegister.Load(this, LeftOperand);
                    Compiler.CallExternal(this, "cate.CompareSignedByte");
                }
            }
        });
    }

    protected override void CompareWord()
    {
        Compare(() =>
        {
            using var lrr = WordOperation.ReserveAnyRegister(this, LeftOperand);
            var leftRegister = lrr.WordRegister;
            leftRegister.Load(this, LeftOperand);
            using var rrr = WordOperation.ReserveAnyRegister(this, RightOperand);
            var rightRegister = rrr.WordRegister;
            rightRegister.Load(this, RightOperand);
            WriteLine("\tsbcw " + leftRegister + "," + rightRegister);
        }, () =>
        {
            var leftRegister = WordRegister.Registers[0];
            using (WordOperation.ReserveRegister(this, leftRegister)) {
                leftRegister.Load(this, RightOperand);
                var rightRegister = WordRegister.Registers[1];
                using (WordOperation.ReserveRegister(this, rightRegister)) {
                    rightRegister.Load(this, LeftOperand);
                    Compiler.CallExternal(this, "cate.CompareSignedWord");
                }
            }
        });
    }

    protected override void ComparePointer()
    {
        Compare(() =>
        {
            using var lrr = PointerOperation.ReserveAnyRegister(this, LeftOperand);
            var leftRegister = lrr.PointerRegister;
            leftRegister.Load(this, LeftOperand);
            using var rrr = PointerOperation.ReserveAnyRegister(this, RightOperand);
            var rightRegister = rrr.PointerRegister;
            rightRegister.Load(this, RightOperand);
            WriteLine("\tsbcw " + leftRegister + "," + rightRegister);
        }, () =>
        {
            var leftRegister = WordPointerRegister.Registers[0];
            using (PointerOperation.ReserveRegister(this, leftRegister)) {
                leftRegister.Load(this, RightOperand);
                var rightRegister = WordPointerRegister.Registers[1];
                using (PointerOperation.ReserveRegister(this, rightRegister)) {
                    rightRegister.Load(this, LeftOperand);
                    Compiler.CallExternal(this, "cate.CompareSignedWord");
                }
            }
        });
    }

    private void Compare(Action compareUnsigned, Action compareSigned)
    {
        {
            switch (OperatorId) {
                case Keyword.Equal:
                    if (RightOperand is not IntegerOperand { IntegerValue: 0 } || !CanOmitOperation(Flag.Z)) {
                        compareUnsigned();
                    }
                    JumpEqual();
                    break;
                case Keyword.NotEqual:
                    if (RightOperand is not IntegerOperand { IntegerValue: 0 } || !CanOmitOperation(Flag.Z)) {
                        compareUnsigned();
                    }

                    JumpNotEqual();
                    break;
                case '<':
                    if (Signed) {
                        compareSigned();
                    }
                    else {
                        compareUnsigned();
                    }
                    JumpLess();
                    break;
                case '>':
                    if (Signed) {
                        compareSigned();
                    }
                    else {
                        compareUnsigned();
                    }
                    JumpGreater();
                    break;
                case Keyword.LessEqual:
                    if (Signed) {
                        compareSigned();
                    }
                    else {
                        compareUnsigned();
                    }
                    JumpLessEqual();
                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        compareSigned();
                    }
                    else {
                        compareUnsigned();
                    }
                    JumpGreaterEqual();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }


    }


    private void JumpEqual()
    {
        WriteJumpLine("\tjr z," + Anchor);
    }
    private void JumpNotEqual()
    {
        WriteJumpLine("\tjr nz," + Anchor);
    }
    private void JumpLess()
    {
        WriteJumpLine("\tjr c," + Anchor);
    }
    private void JumpGreater()
    {
        WriteJumpLine("\tjr z," + Anchor + "_F" + subLabelIndex);
        WriteJumpLine("\tjr nc," + Anchor);
        WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
        ++subLabelIndex;
    }
    private void JumpLessEqual()
    {
        WriteJumpLine("\tjr z," + Anchor);
        WriteJumpLine("\tjr c," + Anchor);
    }
    private void JumpGreaterEqual()
    {
        WriteJumpLine("\tjr nc," + Anchor);
    }
}