using System;

namespace Inu.Cate.I8080;

internal class CompareInstruction : Cate.CompareInstruction
{
    private static int subLabelIndex = 0;

    public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor) { }

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
        if (RightOperand is IntegerOperand { IntegerValue: 0 } && OperatorId is Keyword.Equal or Keyword.NotEqual) {
            CompareByteZero();
            Jump(true);
            return;
        }

        if (Signed && OperatorId != Keyword.Equal && OperatorId != Keyword.NotEqual) {
            using (ByteOperation.ReserveRegister(this, ByteRegister.E, RightOperand)) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
                    ByteRegister.A.Load(this, LeftOperand);
                    ByteRegister.E.Load(this, RightOperand);
                    Compiler.CallExternal(this, "cate.CompareAESigned");
                }
            }
            Jump(false);
            return;
        }

        const string operation = "cmp|cpi";
        if (LeftOperand is VariableOperand variableOperand) {
            if (VariableRegisterMatches(variableOperand, ByteRegister.A)) {
                if (RightOperand is IndirectOperand indirectOperand && indirectOperand.Offset != 0) {
                    using var reservation = ByteOperation.ReserveAnyRegister(this, ByteOperation.RegistersOtherThan(ByteRegister.A));
                    reservation.ByteRegister.LoadIndirect(this, indirectOperand.Variable, indirectOperand.Offset);
                    WriteLine("\tcmp\t" + reservation.ByteRegister.Name);
                }
                else {
                    ByteRegister.A.Operate(this, operation, false, RightOperand);
                }
                goto jump;
            }
        }
        {
            if (RightOperand is IndirectOperand indirectOperand && indirectOperand.Offset != 0) {
                using var reservation = ByteOperation.ReserveAnyRegister(this, ByteOperation.RegistersOtherThan(ByteRegister.A));
                reservation.ByteRegister.LoadIndirect(this, indirectOperand.Variable, indirectOperand.Offset);
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    ByteRegister.A.Load(this, LeftOperand);
                    WriteLine("\tcmp\t" + reservation.ByteRegister.Name);
                }
            }
            else {
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    ByteRegister.A.Load(this, LeftOperand);
                    ByteRegister.A.Operate(this, operation, false, RightOperand);
                }
            }
        }
        jump:
        Jump(false);
    }

    private void CompareByteZero()
    {
        if (LeftOperand is VariableOperand leftVariableOperand) {
            if (VariableRegisterMatches(leftVariableOperand, ByteRegister.A)) {
                if ((OperatorId == Keyword.Equal || OperatorId == Keyword.NotEqual) && CanOmitOperation(Flag.Z))
                    return;
                WriteLine("\tora\ta");
                return;
            }
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            ByteRegister.A.Load(this, LeftOperand);
            WriteLine("\tora\ta");
        }
    }

    private void CompareHlDe()
    {
        Compiler.CallExternal(this, Signed ? "cate.CompareHlDeSigned" : "cate.CompareHlDe");
    }

    protected override void CompareWord()
    {

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

        if (Equals(RightOperand.Register, WordRegister.De)) {
            CompareDe();
        }
        else {
            using var reservation = WordOperation.ReserveRegister(this, WordRegister.De, RightOperand);
            WordRegister.De.Load(this, RightOperand);
            CompareDe();
        }
        Jump(false);
    }

    protected override void ComparePointer()
    {
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

        if (Equals(RightOperand.Register, PointerRegister.De)) {
            CompareDe();
        }
        else {
            using var reservation = PointerOperation.ReserveRegister(this, PointerRegister.De, RightOperand);
            PointerRegister.De.Load(this, RightOperand);
            CompareDe();
        }
        Jump(false);
    }

    private void Jump(bool operandZero)
    {
        switch (OperatorId) {
            case Keyword.Equal:
                WriteJumpLine("\tjz\t" + Anchor);
                break;
            case Keyword.NotEqual:
                WriteJumpLine("\tjnz\t" + Anchor);
                break;
            case '<':
                if (Signed && operandZero) {
                    WriteJumpLine("\tjm\t" + Anchor);
                }
                else {
                    WriteJumpLine("\tjc\t" + Anchor);
                }
                break;
            case '>':
                WriteJumpLine("\tjz\t" + Anchor + "_F" + subLabelIndex);
                if (Signed && operandZero) {
                    WriteJumpLine("\tjp\t" + Anchor);
                }
                else {
                    WriteJumpLine("\tjnc\t" + Anchor);
                }
                WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                ++subLabelIndex;
                break;
            case Keyword.LessEqual:
                WriteJumpLine("\tjz\t" + Anchor);
                if (Signed && operandZero) {
                    WriteJumpLine("\tjm\t" + Anchor);
                }
                else {
                    WriteJumpLine("\tjc\t" + Anchor);
                }
                break;
            case Keyword.GreaterEqual:
                if (Signed && operandZero) {
                    WriteJumpLine("\tjp\t" + Anchor);
                }
                else {
                    WriteJumpLine("\tjnc\t" + Anchor);
                }
                break;
            default:
                throw new NotImplementedException();
        }
    }
}