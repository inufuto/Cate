﻿using System;

namespace Inu.Cate.Mc6809
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor)
        { }

        protected override void CompareByte()
        {
            if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
                if (OperatorId is Keyword.Equal or Keyword.NotEqual) {
                    if (LeftOperand is VariableOperand variableOperand) {
                        var registerId = GetVariableRegister(variableOperand);
                        if (registerId != null) {
                            if (CanOmitOperation(Flag.Z)) {
                                goto jump;
                            }
                        }
                    }
                    ByteOperation.Operate(this, "tst", false, LeftOperand);
                    goto jump;
                }
            }

            using (var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand)) {
                var register = reservation.ByteRegister;
                register.Load(this, LeftOperand);
                register.Operate(this, "cmp", false, RightOperand);
            }
        jump:
            Jump();
        }


        protected override void CompareWord()
        {
            if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
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
            using (var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand)) {
                var register = reservation.WordRegister;
                register.Load(this, LeftOperand);
                register.Operate(this, "cmp", false, RightOperand);
            }
        jump:
            Jump();
        }

        protected override void ComparePointer()
        {
            if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
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

            if (LeftOperand.Register is PointerRegister leftRegister) {
                ViaRegister(leftRegister);
                goto jump;
            }
            using (var reservation = PointerOperation.ReserveAnyRegister(this, LeftOperand)) {
                var register = reservation.PointerRegister;
                ViaRegister(register);
            }
        jump:
            Jump();
            return;

            void ViaRegister(Cate.PointerRegister register)
            {
                register.Load(this, LeftOperand);
                register.Operate(this, "cmp", false, RightOperand);
            }
        }

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
                        WriteJumpLine("\tblt\t" + Anchor);
                    }
                    else {
                        WriteJumpLine("\tbcs\t" + Anchor);
                    }

                    break;
                case '>':
                    if (Signed) {
                        WriteJumpLine("\tbgt\t" + Anchor);
                    }
                    else {
                        WriteJumpLine("\tbhi\t" + Anchor);
                    }

                    break;
                case Keyword.LessEqual:
                    if (Signed) {
                        WriteJumpLine("\tble\t" + Anchor);
                    }
                    else {
                        WriteJumpLine("\tbls\t" + Anchor);
                    }

                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        WriteJumpLine("\tbge\t" + Anchor);
                    }
                    else {
                        WriteJumpLine("\tbcc\t" + Anchor);
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}