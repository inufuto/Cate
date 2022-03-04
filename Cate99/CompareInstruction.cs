using System;
using System.Diagnostics;
using System.Linq;

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
                goto jump;
            }

            var left = Tms99.Compiler.OperandToString(this, LeftOperand);
            var right = Tms99.Compiler.OperandToString(this, RightOperand);
            if (left != null) {
                if (right != null) {
                    WriteLine("\tcb\t" + left + "," + right);
                    goto jump;
                }
                ByteOperation.UsingAnyRegister(this, rightRegister =>
                {
                    rightRegister.Load(this, RightOperand);
                    WriteLine("\tcb\t" + left + "," + rightRegister.Name);
                });
                goto jump;
            }
            ByteOperation.UsingAnyRegister(this, leftRegister =>
            {
                leftRegister.Load(this, LeftOperand);
                if (right != null) {
                    WriteLine("\tcb\t" + leftRegister.Name + "," + right);
                }
                else {
                    ByteOperation.UsingAnyRegister(this, rightRegister =>
                    {
                        rightRegister.Load(this, RightOperand);
                        this.WriteLine("\tcb\t" + leftRegister.Name + "," + rightRegister.Name);
                    });
                }
            });

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
            else if (RightOperand is PointerOperand pointerOperand) {
                Tms99.WordOperation.OperateConstant(this, "ci", LeftOperand, pointerOperand.MemoryAddress());
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
