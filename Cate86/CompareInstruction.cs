﻿using System;

namespace Inu.Cate.I8086
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand,
            Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor)
        {
        }

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

            using (var reservation = ByteOperation.ReserveAnyRegister(this, ByteRegister.Registers, LeftOperand)) {
                var temporaryRegister = reservation.ByteRegister;
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, "cmp ", false, RightOperand);
            }
        jump:
            Jump();
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

            using (var reservation = WordOperation.ReserveAnyRegister(this, WordOperation.Registers, LeftOperand)) {
                var temporaryRegister = reservation.WordRegister;
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, "cmp ", false, RightOperand);
            }
        jump:
            Jump();
        }

        protected override void ComparePointer()
        {
            if (LeftOperand.Register != null && RightOperand is IntegerOperand { IntegerValue: 0 }) {
                WriteLine("\tor " + LeftOperand.Register + "," + LeftOperand.Register);
                goto jump;
            }
            if (LeftOperand is VariableOperand { Register: null } leftVariableOperand) {
                switch (RightOperand) {
                    case ConstantOperand constantOperand:
                        WriteLine("\tcmp word ptr [" + leftVariableOperand.MemoryAddress() + "]," +
                                  constantOperand.MemoryAddress());
                        goto jump;
                    case VariableOperand { Register: { } } rightVariableOperand:
                        WriteLine("\tcmp [" + leftVariableOperand.MemoryAddress() + "]," + rightVariableOperand.Register);
                        goto jump;
                }
            }
            using (var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers, LeftOperand)) {
                var temporaryRegister = reservation.PointerRegister;
                temporaryRegister.Load(this, LeftOperand);
                switch (RightOperand) {
                    case ConstantOperand constantOperand: {
                            WriteLine("\tcmp\t" + temporaryRegister + "," + constantOperand.MemoryAddress());
                            break;
                        }
                    case VariableOperand variableOperand: {
                            var register = GetVariableRegister(variableOperand);
                            if (register is PointerRegister pointerRegister) {
                                WriteLine("\tcmp\t" + temporaryRegister + "," + pointerRegister);
                            }
                            else {
                                WriteLine("\tcmp\t" + temporaryRegister + ",[" +
                                          variableOperand.Variable.MemoryAddress(variableOperand.Offset) + "]");
                            }
                            break;
                        }
                }
            }
        jump:
            Jump();
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
}
