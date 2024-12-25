namespace Inu.Cate.Wdc65816;

internal class CompareInstruction(
    Function function,
    int operatorId,
    Operand leftOperand,
    Operand rightOperand,
    Anchor anchor)
    : Cate.CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor)
{
    private static int subLabelIndex = 0;

    //public override void BuildAssembly()
    //{
    //    if (OperandZero()) {
    //        if (OperatorId is Keyword.Equal or Keyword.NotEqual) {
    //            if (LeftOperand is VariableOperand variableOperand) {
    //                var registerId = GetVariableRegister(variableOperand);
    //                if (registerId != null) {
    //                    if (CanOmitOperation(Flag.Z)) {
    //                        Jump();
    //                        return;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    base.BuildAssembly();
    //}

    private void Jump()
    {
        switch (OperatorId) {
            case Keyword.Equal:
                WriteJumpLine("\tbeq\t" + Anchor);
                break;
            case Keyword.NotEqual:
                WriteJumpLine("\tbne\t" + Anchor);
                break;
            case '<':
                if (Signed) {
                    BranchLessThan(OperandZero(), WriteJumpLine);
                }
                else {
                    WriteJumpLine("\tbcc\t" + Anchor);
                }
                break;
            case '>':
                if (Signed) {
                    BranchGreaterThan(OperandZero(), WriteJumpLine);
                }
                else {
                    BranchHigherThan(WriteJumpLine);
                }
                break;
            case Keyword.LessEqual:
                if (Signed) {
                    BranchLessThanOrEqualTo(OperandZero(), WriteJumpLine);
                }
                else {
                    BranchLowerThanOrSameTo(WriteJumpLine);
                }
                break;
            case Keyword.GreaterEqual:
                if (Signed) {
                    BranchGreaterThanOrEqualTo(OperandZero(), WriteJumpLine);
                }
                else {
                    WriteJumpLine("\tbcs\t" + Anchor);
                }
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void BranchLowerThanOrSameTo(Action<string> write)
    {
        write("\tbeq\t" + Anchor);
        write("\tbcc\t" + Anchor);
    }

    private void BranchHigherThan(Action<string> write)
    {
        write("\tbeq\t" + Anchor + "_F" + subLabelIndex);
        write("\tbcs\t" + Anchor);
        write(Anchor + "_F" + subLabelIndex + ":");
        ++subLabelIndex;
    }

    private void BranchLessThan(bool operandZero, Action<string> write)
    {
        if (operandZero) {
            write("\tbmi\t" + Anchor);
        }
        else {
            write("\tbvs\t" + Anchor + "_OF" + subLabelIndex);
            write("\tbmi\t" + Anchor);
            write("\tjmp\t" + Anchor + "_F" + subLabelIndex);
            write(Anchor + "_OF" + subLabelIndex + ":");
            write("\tbpl\t" + Anchor);
        }
        write(Anchor + "_F" + subLabelIndex + ":");
        ++subLabelIndex;
    }

    private void BranchLessThanOrEqualTo(bool operandZero, Action<string> write)
    {
        write("\tbeq\t" + Anchor);
        BranchLessThan(operandZero, write);
    }


    private void BranchGreaterThanOrEqualTo(bool operandZero, Action<string> write)
    {
        if (operandZero) {
            write("\tbpl\t" + Anchor);
        }
        else {
            write("\tbvs\t" + Anchor + "_OF" + subLabelIndex);
            write("\tbpl\t" + Anchor);
            write("\tjmp\t" + Anchor + "_F" + subLabelIndex);
            write(Anchor + "_OF" + subLabelIndex + ":");
            write("\tbmi\t" + Anchor);
        }
        write(Anchor + "_F" + subLabelIndex + ":");
        ++subLabelIndex;
    }

    private void BranchGreaterThan(bool operandZero, Action<string> write)
    {
        write("\tbeq\t" + Anchor + "_F" + subLabelIndex);
        BranchGreaterThanOrEqualTo(operandZero, write);
    }

    private bool OperandZero()
    {
        return RightOperand is IntegerOperand { IntegerValue: 0 };
    }

    protected override void CompareByte()
    {
        var candidates = (RightOperand is IndirectOperand || LeftOperand is IndirectOperand) ? [ByteRegister.A] : ByteRegister.Registers;
        using (var reservation = ByteOperation.ReserveAnyRegister(this, candidates, LeftOperand)) {
            var register = reservation.ByteRegister;
            register.Load(this, LeftOperand);
            if (!OperandZero() || OperatorId is not (Keyword.Equal or Keyword.NotEqual)) {
                var operation = Equals(register, ByteRegister.A) ? "cmp" : "cp" + register.Name;
                register.Operate(this, operation, false, RightOperand);
            }
        }
        Jump();
    }

    protected override void CompareWord()
    {
        var candidates = (RightOperand is IndirectOperand || LeftOperand is IndirectOperand) ? [WordRegister.A] : (List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y];
        using (var reservation = WordOperation.ReserveAnyRegister(this, candidates, LeftOperand)) {
            var register = reservation.WordRegister;
            register.Load(this, LeftOperand);
            if (!OperandZero() || OperatorId is not (Keyword.Equal or Keyword.NotEqual)) {
                register.Compare(this, "cmp", RightOperand);
            }
        }
        Jump();
    }

    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (Equals(register, ByteRegister.A) || Equals(register, WordRegister.A)) {
            return null;
        }
        switch (RightOperand) {
            case IndirectOperand { Variable.Register: null } when register is WordIndexRegister:
            case VariableOperand variableOperand when variableOperand.Variable.Equals(variable) && register is WordRegister or ByteRegister:
                return null;
        }
        return base.RegisterAdaptability(variable, register);
    }
}