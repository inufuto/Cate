using Microsoft.Win32;

namespace Inu.Cate.Sm83;

internal class CompareInstruction(
    Function function,
    int operatorId,
    Operand leftOperand,
    Operand rightOperand,
    Anchor anchor)
    : Cate.CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor)
{
    private static int subLabelIndex = 0;

    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (Equals(register, ByteRegister.A) && RightOperand is VariableOperand variableOperand &&
            variableOperand.Variable == variable) {
            return null;
        }
        return base.RegisterAdaptability(variable, register);
    }

    protected override void CompareByte()
    {
        var operandZero = false;
        if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
            if (OperatorId is Keyword.Equal or Keyword.NotEqual) {
                CompareByteZero();
                operandZero = true;
                goto jump;
            }
            if (Signed) {
                switch (OperatorId) {
                    case '<':
                        ViaRegister(register =>
                        {
                            WriteLine("\tbit\t7," + register.AsmName);
                            WriteJumpLine("\tjr\tnz," + Anchor);
                        });
                        return;
                    case Keyword.GreaterEqual:
                        ViaRegister(register =>
                        {
                            WriteLine("\tbit\t7," + register.AsmName);
                            WriteJumpLine("\tjr\tz," + Anchor);
                        });
                        return;
                }
                void ViaRegister(Action<Cate.ByteRegister> action)
                {
                    using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
                    reservation.ByteRegister.Load(this, LeftOperand);
                    action(reservation.ByteRegister);
                }
            }
        }

        if (Signed) {
            if (IsRegisterReserved(ByteRegister.C)) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.E, RightOperand)) {
                    ByteRegister.E.Load(this, RightOperand);
                    using (ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
                        ByteRegister.A.Load(this, LeftOperand);
                        Compiler.CallExternal(this, "cate.CompareAeSigned");
                    }
                }
            }
            else {
                using (ByteOperation.ReserveRegister(this, ByteRegister.C, RightOperand)) {
                    ByteRegister.C.Load(this, RightOperand);
                    using (ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
                        ByteRegister.A.Load(this, LeftOperand);
                        Compiler.CallExternal(this, "cate.CompareAcSigned");
                    }
                }
            }
        }
        else {
            const string operation = "cp";
            if (LeftOperand is VariableOperand variableOperand) {
                if (VariableRegisterMatches(variableOperand, ByteRegister.A)) {
                    if (RightOperand is IndirectOperand indirectOperand && indirectOperand.Offset != 0) {
                        using var reservation = ByteOperation.ReserveAnyRegister(this, ByteOperation.RegistersOtherThan(ByteRegister.A));
                        reservation.ByteRegister.LoadIndirect(this, indirectOperand.Variable, indirectOperand.Offset);
                        WriteLine("\tcp\ta," + reservation.ByteRegister.Name);
                    }
                    else {
                        ByteRegister.A.Operate(this, operation, false, RightOperand);
                    }
                    goto jump;
                }
            }
            {
                if (RightOperand is IndirectOperand indirectOperand && indirectOperand.Offset != 0) {
                    using var reservation =
                        ByteOperation.ReserveAnyRegister(this, ByteOperation.RegistersOtherThan(ByteRegister.A));
                    reservation.ByteRegister.LoadIndirect(this, indirectOperand.Variable, indirectOperand.Offset);
                    using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                        ByteRegister.A.Load(this, LeftOperand);
                        WriteLine("\tcp\ta," + reservation.ByteRegister.Name);
                    }
                }
                else {
                    using (ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
                        ByteRegister.A.Load(this, LeftOperand);
                        ByteRegister.A.Operate(this, operation, false, RightOperand);
                    }
                }
            }
        }
    jump:
        Jump(operandZero);
    }

    private void CompareByteZero()
    {
        if (LeftOperand is VariableOperand leftVariableOperand) {
            if (VariableRegisterMatches(leftVariableOperand, ByteRegister.A)) {
                if (OperatorId is Keyword.Equal or Keyword.NotEqual && CanOmitOperation(Flag.Z))
                    return;
                WriteLine("\tor\ta,a");
                return;
            }
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            ByteRegister.A.Load(this, LeftOperand);
            WriteLine("\tor\ta,a");
        }
    }

    private void CompareHlDe()
    {
        Compiler.CallExternal(this, Signed ? "cate.CompareHlDeSigned" : "cate.CompareHlDe");
    }

    protected override void CompareWord()
    {
        if (Equals(RightOperand.Register, WordRegister.De)) {
            CompareDe();
        }
        else {
            using var reservation = WordOperation.ReserveRegister(this, WordRegister.De, RightOperand);
            WordRegister.De.Load(this, RightOperand);
            CompareDe();
        }
        Jump(false);
        return;

        void CompareDe()
        {
            if (Equals(LeftOperand.Register, WordRegister.Hl)) {
                CompareHlDe();
            }
            else {
                using var reservation = WordOperation.ReserveRegister(this, WordRegister.Hl);
                WordRegister.Hl.Load(this, LeftOperand);
                CompareHlDe();
            }
        }
    }

    protected override void ComparePointer()
    {
        if (Equals(RightOperand.Register, PointerRegister.De)) {
            CompareDe();
        }
        else {
            using var reservation = PointerOperation.ReserveRegister(this, PointerRegister.De, RightOperand);
            PointerRegister.De.Load(this, RightOperand);
            CompareDe();
        }
        Jump(false);
        return;

        void CompareDe()
        {
            if (Equals(LeftOperand.Register, PointerRegister.Hl)) {
                CompareHlDe();
            }
            else {
                using var reservation = PointerOperation.ReserveRegister(this, PointerRegister.Hl);
                PointerRegister.Hl.Load(this, LeftOperand);
                CompareHlDe();
            }
        }
    }

    private void Jump(bool operandZero)
    {
        switch (OperatorId) {
            case Keyword.Equal:
                WriteJumpLine("\tjr\tz," + Anchor);
                break;
            case Keyword.NotEqual:
                WriteJumpLine("\tjr\tnz," + Anchor);
                break;
            case '<':
                WriteJumpLine("\tjr\tc," + Anchor);
                break;
            case '>':
                WriteJumpLine("\tjr\tz," + Anchor + "_F" + subLabelIndex);
                WriteJumpLine("\tjr\tnc," + Anchor);
                WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                ++subLabelIndex;
                break;
            case Keyword.LessEqual:
                WriteJumpLine("\tjr\tz," + Anchor);
                WriteJumpLine("\tjr\tc," + Anchor);
                break;
            case Keyword.GreaterEqual:
                WriteJumpLine("\tjr\tnc," + Anchor);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}