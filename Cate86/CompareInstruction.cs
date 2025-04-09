using System;

namespace Inu.Cate.I8086;

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
        if (LeftOperand.Register != null && RightOperand is IntegerOperand { IntegerValue: 0 }) {
            WriteLine("\tor " + LeftOperand.Register + "," + LeftOperand.Register);
            goto jump;
        }

        if (LeftOperand is VariableOperand { Register: null } leftVariableOperand) {
            if (RightOperand is ConstantOperand constantOperand) {
                WriteLine("\tcmp byte ptr [" + leftVariableOperand.MemoryAddress() + "]," +
                          constantOperand.MemoryAddress());
                goto jump;
            }

            if (RightOperand is VariableOperand { Register: { } } rightVariableOperand) {
                WriteLine("\tcmp [" + leftVariableOperand.MemoryAddress() + "]," + rightVariableOperand.Register);
                goto jump;
            }
        }

        if (LeftOperand.Register is ByteRegister leftRegister) {
            ViaRegister(leftRegister);
        }
        else {
            using var reservation = ByteOperation.ReserveAnyRegister(this, ByteRegister.Registers, LeftOperand);
            ViaRegister(reservation.ByteRegister);
        }

        jump:
        Jump();
        return;

        void ViaRegister(Cate.ByteRegister register)
        {
            register.Load(this, LeftOperand);
            register.Operate(this, "cmp ", false, RightOperand);
        }
    }

    protected override void CompareWord()
    {
        if (LeftOperand.Register != null && RightOperand is IntegerOperand { IntegerValue: 0 }) {
            WriteLine("\tor " + LeftOperand.Register + "," + LeftOperand.Register);
            goto jump;
        }

        if (LeftOperand is VariableOperand { Register: null } leftVariableOperand) {
            if (RightOperand is ConstantOperand constantOperand) {
                WriteLine("\tcmp word ptr [" + leftVariableOperand.MemoryAddress() + "]," +
                          constantOperand.MemoryAddress());
                goto jump;
            }

            if (RightOperand is VariableOperand { Register: { } } rightVariableOperand) {
                WriteLine("\tcmp [" + leftVariableOperand.MemoryAddress() + "]," + rightVariableOperand.Register);
                goto jump;
            }
        }

        if (LeftOperand.Register is WordRegister leftRegister) {
            ViaRegister(leftRegister);
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(this, WordOperation.Registers, LeftOperand);
            ViaRegister(reservation.WordRegister);
        }

        jump:
        Jump();
        return;

        void ViaRegister(Cate.WordRegister register)
        {
            register.Load(this, LeftOperand);
            register.Operate(this, "cmp ", false, RightOperand);
        }
    }


    private void Jump()
    {
        switch (OperatorId) {
            case Keyword.Equal:
                WriteJumpLine("\tje " + Anchor);
                break;
            case Keyword.NotEqual:
                WriteJumpLine("\tjne " + Anchor);
                break;
            case '<':
                if (Signed) {
                    WriteJumpLine("\tjl " + Anchor);
                }
                else {
                    WriteJumpLine("\tjb " + Anchor);
                }

                break;
            case '>':
                if (Signed) {
                    WriteJumpLine("\tjg " + Anchor);
                }
                else {
                    WriteJumpLine("\tja " + Anchor);
                }

                break;
            case Keyword.LessEqual:
                if (Signed) {
                    WriteJumpLine("\tjle " + Anchor);
                }
                else {
                    WriteJumpLine("\tjbe " + Anchor);
                }

                break;
            case Keyword.GreaterEqual:
                if (Signed) {
                    WriteJumpLine("\tjge " + Anchor);
                }
                else {
                    WriteJumpLine("\tjae " + Anchor);
                }

                break;
            default:
                throw new NotImplementedException();
        }
    }
}