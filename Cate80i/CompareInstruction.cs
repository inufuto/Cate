using System;
using System.Diagnostics;

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
            if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
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
                GetVariableRegister(variableOperand);
                if (VariableRegisterMatches(variableOperand, ByteRegister.A)) {
                    ByteRegister.A.Operate(this, operation, false, RightOperand);
                    goto jump;
                }
            }
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, LeftOperand);
                ByteRegister.A.Operate(this, operation, false, RightOperand);
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

        protected override void CompareWord()
        {
            if (Signed && OperatorId != Keyword.Equal && OperatorId != Keyword.NotEqual) {
                using (WordOperation.ReserveRegister(this, WordRegister.De, RightOperand)) {
                    using (WordOperation.ReserveRegister(this, WordRegister.Hl, LeftOperand)) {
                        WordRegister.De.Load(this, RightOperand);
                        WordRegister.Hl.Load(this, LeftOperand);
                        Compiler.CallExternal(this, "cate.CompareHlDeSigned");
                    }
                }
                Jump(false);
                return;
            }

            if (RightOperand is IntegerOperand { IntegerValue: 0 } || RightOperand is NullPointerOperand) {
                if (LeftOperand.Register is WordRegister leftRegister) {
                    if (leftRegister.IsPair()) {
                        CompareWordZero(leftRegister);
                        Jump(false);
                        return;
                    }
                }
                using (var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers)) {
                    var temporaryRegister = reservation.WordRegister;
                    temporaryRegister.Load(this, LeftOperand);
                    CompareWordZero(temporaryRegister);
                }
                Jump(false);
                return;
            }

            using (WordOperation.ReserveRegister(this, WordRegister.De)) {
                WordRegister.De.Load(this, RightOperand);
                if (Equals(LeftOperand.Register, WordRegister.Hl)) {
                    Compiler.CallExternal(this, "cate.CompareHlDe");
                }
                else {
                    using (WordOperation.ReserveRegister(this, WordRegister.Hl)) {
                        WordRegister.Hl.Load(this, LeftOperand);
                        Compiler.CallExternal(this, "cate.CompareHlDe");
                    }
                }
            }
            Jump(false);
        }

        private void CompareWordZero(Cate.WordRegister leftRegister)
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                Debug.Assert(leftRegister.Low != null);
                Debug.Assert(leftRegister.High != null);
                ByteRegister.A.CopyFrom(this, leftRegister.Low);
                WriteLine("\tora\t" + leftRegister.High.Name);
                AddChanged(ByteRegister.A);
                RemoveRegisterAssignment(ByteRegister.A);
            }
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
}
