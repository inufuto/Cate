using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Inu.Cate.I8080
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        private static int subLabelIndex = 0;

        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor) { }

        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            if (Equals(register, ByteRegister.A) && RightOperand is VariableOperand variableOperand &&
                variableOperand.Variable == variable) {
                return false;
            }
            return base.CanAllocateRegister(variable, register);
        }


        protected override void CompareByte()
        {
            var operandZero = false;
            if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
                CompareByteZero();
                operandZero = true;
                goto jump;
            }

            const string operation = "cmp|cpi";
            if (LeftOperand is VariableOperand variableOperand) {
                GetVariableRegister(variableOperand);
                if (VariableRegisterMatches(variableOperand, ByteRegister.A)) {
                    ByteRegister.A.Operate(this, operation, false, RightOperand);
                    goto jump;
                }
            }
            BeginRegister(ByteRegister.A);
            ByteRegister.A.Load(this, LeftOperand);
            ByteRegister.A.Operate(this, operation, false, RightOperand);
            EndRegister(ByteRegister.A);

        jump:
            Jump(operandZero);
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
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, LeftOperand);
                WriteLine("\tora\ta");
            });
        }

        protected override void CompareWord()
        {
            if (RightOperand is IntegerOperand { IntegerValue: 0 } || RightOperand is NullPointerOperand) {
                if (LeftOperand.Register is WordRegister leftRegister) {
                    if (leftRegister.IsPair()) {
                        CompareWordZero(leftRegister);
                        goto jump;
                    }
                }
                WordOperation.UsingAnyRegister(this, WordRegister.Registers, temporaryRegister =>
                {
                    temporaryRegister.Load(this, LeftOperand);
                    CompareWordZero(temporaryRegister);
                });
                goto jump;
            }
            WordOperation.UsingRegister(this, WordRegister.De, () =>
            {
                WordRegister.De.Load(this, RightOperand);
                if (Equals(LeftOperand.Register, WordRegister.Hl)) {
                    Compiler.CallExternal(this, "cate.CompareHlDe");
                }
                else {
                    WordOperation.UsingRegister(this, WordRegister.Hl, () =>
                    {
                        WordRegister.Hl.Load(this, LeftOperand);
                        Compiler.CallExternal(this, "cate.CompareHlDe");
                    });
                }
            });
        jump:
            Jump(false);
        }

        private void CompareWordZero(Cate.WordRegister leftRegister)
        {
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                Debug.Assert(leftRegister.Low != null);
                Debug.Assert(leftRegister.High != null);
                ByteRegister.A.CopyFrom(this, leftRegister.Low);
                WriteLine("\tora\t" + leftRegister.High.Name);
                ChangedRegisters.Add(ByteRegister.A);
                RemoveRegisterAssignment(ByteRegister.A);
            });
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
                    if (Signed) {
                        if (operandZero) {
                            WriteJumpLine("\tjm\t" + Anchor);
                        }
                        else {
                            WriteJumpLine("\tjpe\t" + Anchor + "_OF" + subLabelIndex);
                            WriteJumpLine("\tjm\t" + Anchor);
                            WriteJumpLine("\tjmp\t" + Anchor + "_F" + subLabelIndex);
                            WriteJumpLine(Anchor + "_OF" + subLabelIndex + ":");
                            WriteJumpLine("\tjp\t" + Anchor);
                            WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                            ++subLabelIndex;
                        }
                    }
                    else {
                        WriteJumpLine("\tjc\t" + Anchor);
                    }
                    break;
                case '>':
                    WriteJumpLine("\tjz\t" + Anchor + "_F" + subLabelIndex);
                    if (Signed) {
                        if (operandZero) {
                            WriteJumpLine("\tjp\t" + Anchor);
                        }
                        else {
                            WriteJumpLine("\tjpe\t" + Anchor + "_OF" + subLabelIndex);
                            WriteJumpLine("\tjp\t" + Anchor);
                            WriteJumpLine("\tjmp\t" + Anchor + "_F" + subLabelIndex);
                            WriteJumpLine(Anchor + "_OF" + subLabelIndex + ":");
                            WriteJumpLine("\tjm\t" + Anchor);
                        }
                    }
                    else {
                        WriteJumpLine("\tjnc\t" + Anchor);
                    }
                    WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                    ++subLabelIndex;
                    break;
                case Keyword.LessEqual:
                    WriteJumpLine("\tjz\t" + Anchor);
                    if (Signed) {
                        if (operandZero) {
                            WriteJumpLine("\tjm\t" + Anchor);
                        }
                        else {
                            WriteJumpLine("\tjpe\t" + Anchor + "_OF" + subLabelIndex);
                            WriteJumpLine("\tjm\t" + Anchor);
                            WriteJumpLine("\tjmp\t" + Anchor + "_F" + subLabelIndex);
                            WriteJumpLine(Anchor + "_OF" + subLabelIndex + ":");
                            WriteJumpLine("\tjp\t" + Anchor);
                            WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                            ++subLabelIndex;
                        }
                    }
                    else {
                        WriteJumpLine("\tjc\t" + Anchor);
                    }
                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        if (operandZero) {
                            WriteJumpLine("\tjp\t" + Anchor);
                        }
                        else {
                            WriteJumpLine("\tjpe\t" + Anchor + "_OF" + subLabelIndex);
                            WriteJumpLine("\tjp\t" + Anchor);
                            WriteJumpLine("\tjmp\t" + Anchor + "_F" + subLabelIndex);
                            WriteJumpLine(Anchor + "_OF" + subLabelIndex + ":");
                            WriteJumpLine("\tjm\t" + Anchor);
                            WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                            ++subLabelIndex;
                        }
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
}
