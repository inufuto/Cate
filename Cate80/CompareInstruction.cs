using System;

namespace Inu.Cate.Z80
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        private static int subLabelIndex = 0;

        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor)
            : base(function, operatorId, leftOperand, rightOperand, anchor) { }

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

            const string operation = "cp\t";
            if (LeftOperand is VariableOperand variableOperand) {
                var variableRegister = GetVariableRegister(variableOperand);
                if (VariableRegisterMatches(variableOperand, ByteRegister.A)) {
                    ByteRegister.A.Operate(this, operation, false, RightOperand);
                    goto jump;
                }
            }

            using (var reservation = ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
                ByteRegister.A.Load(this, LeftOperand);
                ByteRegister.A.Operate(this, operation, false, RightOperand);
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
                    WriteLine("\tor\ta");
                    return;
                }
            }
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, LeftOperand);
                WriteLine("\tor\ta");
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
                    WriteJumpLine("\tjr\tz," + Anchor);
                    break;
                case Keyword.NotEqual:
                    WriteJumpLine("\tjr\tnz," + Anchor);
                    break;
                case '<':
                    if (Signed) {
                        if (operandZero) {
                            WriteJumpLine("\tjp\tm," + Anchor);
                        }
                        else {
                            WriteJumpLine("\tjp\tpe," + Anchor + "_OF" + subLabelIndex);
                            WriteJumpLine("\tjp\tm," + Anchor);
                            WriteJumpLine("\tjp\t" + Anchor + "_F" + subLabelIndex);
                            WriteJumpLine(Anchor + "_OF" + subLabelIndex + ":");
                            WriteJumpLine("\tjp\tp," + Anchor);
                            WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                            ++subLabelIndex;
                        }
                    }
                    else {
                        WriteJumpLine("\tjr\tc," + Anchor);
                    }
                    break;
                case '>':
                    WriteJumpLine("\tjr\tz," + Anchor + "_F" + subLabelIndex);
                    if (Signed) {
                        if (operandZero) {
                            WriteJumpLine("\tjp\tp," + Anchor);
                        }
                        else {
                            WriteJumpLine("\tjp\tpe," + Anchor + "_OF" + subLabelIndex);
                            WriteJumpLine("\tjp\tp," + Anchor);
                            WriteJumpLine("\tjp\t" + Anchor + "_F" + subLabelIndex);
                            WriteJumpLine(Anchor + "_OF" + subLabelIndex + ":");
                            WriteJumpLine("\tjp\tm," + Anchor);
                        }
                    }
                    else {
                        WriteJumpLine("\tjr\tnc," + Anchor);
                    }
                    WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                    ++subLabelIndex;
                    break;
                case Keyword.LessEqual:
                    WriteJumpLine("\tjr\tz," + Anchor);
                    if (Signed) {
                        if (operandZero) {
                            WriteJumpLine("\tjp\tm," + Anchor);
                        }
                        else {
                            WriteJumpLine("\tjp\tpe," + Anchor + "_OF" + subLabelIndex);
                            WriteJumpLine("\tjp\tm," + Anchor);
                            WriteJumpLine("\tjp\t" + Anchor + "_F" + subLabelIndex);
                            WriteJumpLine(Anchor + "_OF" + subLabelIndex + ":");
                            WriteJumpLine("\tjp\tp," + Anchor);
                            WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                            ++subLabelIndex;
                        }
                    }
                    else {
                        WriteJumpLine("\tjr\tc," + Anchor);
                    }
                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        if (operandZero) {
                            WriteJumpLine("\tjp\tp," + Anchor);
                        }
                        else {
                            WriteJumpLine("\tjp\tpe," + Anchor + "_OF" + subLabelIndex);
                            WriteJumpLine("\tjp\tp," + Anchor);
                            WriteJumpLine("\tjp\t" + Anchor + "_F" + subLabelIndex);
                            WriteJumpLine(Anchor + "_OF" + subLabelIndex + ":");
                            WriteJumpLine("\tjp\tm," + Anchor);
                            WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
                            ++subLabelIndex;
                        }
                    }
                    else {
                        WriteJumpLine("\tjr\tnc," + Anchor);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
