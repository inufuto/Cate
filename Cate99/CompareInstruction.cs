using System;

namespace Inu.Cate.Tms99
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor) { }

        protected override void CompareByte()
        {
            if (RightOperand is IntegerOperand integerOperand) {
                if (integerOperand.IntegerValue == 0) {
                    if (OperatorId == Keyword.Equal || OperatorId == Keyword.NotEqual) {
                        if (LeftOperand is VariableOperand variableOperand) {
                            var registerId = GetVariableRegister(variableOperand);
                            if (registerId != null) {
                                if (CanOmitOperation(Flag.Z)) {
                                    goto jump;
                                }
                            }
                        }
                    }
                }
                Tms99.ByteOperation.OperateConstant(this, "ci", LeftOperand, integerOperand.IntegerValue);
            }
            else {
                Tms99.ByteOperation.Operate(this, "c", LeftOperand, RightOperand);
            }
            jump:
            Jump();
        }

        protected override void CompareWord()
        {
            if (RightOperand is IntegerOperand integerOperand) {
                if (integerOperand.IntegerValue == 0) {
                    if (OperatorId == Keyword.Equal || OperatorId == Keyword.NotEqual) {
                        if (LeftOperand is VariableOperand variableOperand) {
                            var registerId = GetVariableRegister(variableOperand);
                            if (registerId != null) {
                                if (CanOmitOperation(Flag.Z)) {
                                    goto jump;
                                }
                            }
                        }
                    }
                }
                Tms99.WordOperation.OperateConstant(this, "ci", LeftOperand, integerOperand.IntegerValue);
            }
            else {
                Tms99.WordOperation.Operate(this, "c", LeftOperand, RightOperand);
            }
            jump:
            Jump();
        }

        private void Jump()
        {
            switch (OperatorId) {
                case Keyword.Equal:
                    WriteJumpLine("\tjeq\t" + Anchor);
                    break;
                case Keyword.NotEqual:
                    WriteJumpLine("\tjne\t" + Anchor);
                    break;
                case '<':
                    if (Signed) {
                        WriteJumpLine("\tjlt\t" + Anchor);
                    }
                    else {
                        WriteJumpLine("\tjl\t" + Anchor);
                    }

                    break;
                case '>':
                    if (Signed) {
                        WriteJumpLine("\tjgt\t" + Anchor);
                    }
                    else {
                        WriteJumpLine("\tjh\t" + Anchor);
                    }

                    break;
                case Keyword.LessEqual:
                    if (Signed) {
                        WriteJumpLine("\tjlt\t" + Anchor);
                        WriteJumpLine("\tjeq\t" + Anchor);
                    }
                    else {
                        WriteJumpLine("\tjle\t" + Anchor);
                    }

                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        WriteJumpLine("\tjgt\t" + Anchor);
                        WriteJumpLine("\tjeq\t" + Anchor);
                    }
                    else {
                        WriteJumpLine("\tjhe\t" + Anchor);
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
