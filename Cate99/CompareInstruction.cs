﻿using System;

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

            var left = Tms99.Compiler.OperandToString(this, LeftOperand, false);
            var right = Tms99.Compiler.OperandToString(this, RightOperand, true);
            if (left != null) {
                if (right != null) {
                    WriteLine("\tcb\t" + left + "," + right);
                    goto jump;
                }
                using (var reservation = ByteOperation.ReserveAnyRegister(this)) {
                    var rightRegister = reservation.ByteRegister;
                    rightRegister.Load(this, RightOperand);
                    WriteLine("\tcb\t" + left + "," + rightRegister.Name);
                }
                goto jump;
            }

            using (var leftReservation = ByteOperation.ReserveAnyRegister(this)) {
                var leftRegister = leftReservation.ByteRegister;
                leftRegister.Load(this, LeftOperand);
                if (right != null) {
                    WriteLine("\tcb\t" + leftRegister.Name + "," + right);
                }
                else {
                    using var reserveAnyRegister = ByteOperation.ReserveAnyRegister(this);
                    var rightRegister = reserveAnyRegister.ByteRegister;
                    rightRegister.Load(this, RightOperand);
                    this.WriteLine("\tcb\t" + leftRegister.Name + "," + rightRegister.Name);
                }
            }

        jump:
            Jump();
        }

        protected override void CompareWord()
        {
            if (RightOperand is IntegerOperand integerOperand) {
                if (integerOperand.IntegerValue == 0) {
                    if (OperatorId is Keyword.Equal or Keyword.NotEqual) {
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

        protected override void ComparePointer()
        {
            if (RightOperand is NullPointerOperand) {
                if (OperatorId is Keyword.Equal or Keyword.NotEqual) {
                    if (LeftOperand is VariableOperand variableOperand) {
                        var leftRegister = GetVariableRegister(variableOperand);
                        if (leftRegister != null) {
                            if (CanOmitOperation(Flag.Z)) {
                                goto jump;
                            }
                        }
                    }
                }
                Tms99.PointerOperation.OperateConstant(this, "ci", LeftOperand, "0");
            }
            else if (RightOperand is PointerOperand pointerOperand) {
                Tms99.PointerOperation.OperateConstant(this, "ci", LeftOperand, pointerOperand.MemoryAddress());
            }
            else {
                Tms99.PointerOperation.Operate(this, "c", LeftOperand, RightOperand);
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
