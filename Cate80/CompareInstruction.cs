using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Z80
{
    class CompareInstruction : Cate.CompareInstruction
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
                    WriteLine("\tor\ta");
                    return;
                }
            }
            ByteRegister.UsingAccumulator(this, () =>
            {
                ByteRegister.A.Load(this, LeftOperand);
                WriteLine("\tor\ta");
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
                WordRegister.UsingAny(this, WordRegister.PairRegisters, temporaryRegister =>
                {
                    temporaryRegister.Load(this, LeftOperand);
                    CompareWordZero(temporaryRegister);
                });
                goto jump;
            }

            void Subtract(Cate.WordRegister rightRegister)
            {
                WriteLine("\tor\ta");
                WriteLine("\tsbc\thl," + rightRegister.Name);
                ChangedRegisters.Add(WordRegister.Hl);
                RemoveRegisterAssignment(WordRegister.Hl);
            }


            void SubtractHl()
            {
                if (RightOperand.Register is WordRegister rightRegister) {
                    if (!rightRegister.IsAddable()) {
                        Subtract(rightRegister);
                        return;
                    }
                }
                var candidates = new List<Cate.WordRegister>() { WordRegister.De, WordRegister.Bc }.ToList();
                WordRegister.UsingAny(this, candidates, temporaryRegister =>
                {
                    temporaryRegister.Load(this, RightOperand);
                    Subtract(temporaryRegister);
                });
            }

            if (Equals(LeftOperand.Register, WordRegister.Hl)) {
                BeginRegister(WordRegister.Hl);
                SubtractHl();
                EndRegister(WordRegister.Hl);
                goto jump;
            }
            if (Equals(RightOperand.Register, WordRegister.Hl)) {
                void CompareDeHl()
                {
                    WriteLine("\tex\tde,hl");
                    Subtract(WordRegister.De);
                    WriteLine("\tex\tde,hl");
                    ChangedRegisters.Add(WordRegister.De);
                    RemoveRegisterAssignment(WordRegister.De);
                }

                if (Equals(LeftOperand.Register, WordRegister.De)) {
                    BeginRegister(WordRegister.De);
                    CompareDeHl();
                    EndRegister(WordRegister.De);
                    goto jump;
                }
                WordOperation.UsingRegister(this, WordRegister.De, () =>
                {
                    WordRegister.De.Load(this, LeftOperand);
                    CompareDeHl();
                });
                goto jump;
            }
            WordOperation.UsingRegister(this, WordRegister.Hl, () =>
            {
                WordRegister.Hl.Load(this, LeftOperand);
                SubtractHl();
            });

            jump:
            Jump(false);
        }

        private void CompareWordZero(Cate.WordRegister leftRegister)
        {
            ByteRegister.UsingAccumulator(this, () =>
            {
                Debug.Assert(leftRegister.Low != null);
                Debug.Assert(leftRegister.High != null);
                ByteRegister.A.CopyFrom(this, leftRegister.Low);
                WriteLine("\tor\t" + leftRegister.High.Name);
                ChangedRegisters.Add(ByteRegister.A);
                RemoveRegisterAssignment(ByteRegister.A);
            });
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
