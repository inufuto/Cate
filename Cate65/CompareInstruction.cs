using System;
using System.Collections.Generic;

namespace Inu.Cate.Mos6502
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        private static int subLabelIndex = 0;

        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor)
        { }

        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            {
                if (register is IndexRegister && LeftOperand is VariableOperand variableOperand &&
                    variableOperand.Variable == variable) {
                    return false;
                }
            }
            {
                if (Equals(register, ByteRegister.A) && RightOperand is VariableOperand variableOperand &&
                        variableOperand.Variable == variable) {
                    return false;
                }
            }
            return base.CanAllocateRegister(variable, register);
        }


        public override void BuildAssembly()
        {
            if (LeftOperand.Type.ByteCount == 1) {
                CompareByte();
            }
            else {
                CompareWord();
            }
        }

        protected override void CompareByte()
        {
            var operandZero = RightOperand is IntegerOperand { IntegerValue: 0 };
            if (operandZero) {
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

            List<Cate.ByteRegister> candidates =
                RightOperand is IndirectOperand || LeftOperand is IndirectOperand ?
                new List<Cate.ByteRegister>() { ByteRegister.A } :
                ByteRegister.Registers;
            ByteOperation.UsingAnyRegister(this, candidates, null, LeftOperand, register =>
            {
                var operation = Equals(register, ByteRegister.A) ? "cmp" : "cp" + register.Name;
                register.Load(this, LeftOperand);
                register.Operate(this, operation, false, RightOperand);
            });
            jump:
            switch (OperatorId) {
                case Keyword.Equal:
                    WriteJumpLine("\tbeq\t" + Anchor);
                    break;
                case Keyword.NotEqual:
                    WriteJumpLine("\tbne\t" + Anchor);
                    break;
                case '<':
                    if (Signed) {
                        BranchLessThan(operandZero, WriteJumpLine);
                    }
                    else {
                        WriteJumpLine("\tbcc\t" + Anchor);
                    }
                    break;
                case '>':
                    if (Signed) {
                        BranchGreaterThan(operandZero, WriteJumpLine);
                    }
                    else {
                        BranchHigherThan(WriteJumpLine);
                    }
                    break;
                case Keyword.LessEqual:
                    if (Signed) {
                        BranchLessThanOrEqualTo(operandZero, WriteJumpLine);
                    }
                    else {
                        BranchLowerThanOrSameTo(WriteJumpLine);
                    }
                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        BranchGreaterThanOrEqualTo(operandZero, WriteJumpLine);
                    }
                    else {
                        WriteJumpLine("\tbcs\t" + Anchor);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void BranchLowerThan(Action<string> write)
        {
            write("\tbcc\t" + Anchor);
        }

        private void BranchHigherThanOrSameTo(Action<string> write)
        {
            write("\tbcs\t" + Anchor);
        }

        private void BranchLowerThanOrSameTo(Action<string> write)
        {
            write("\tbeq\t" + Anchor);
            write("\tbcc\t" + Anchor);
        }

        private void BranchHigherThan(Action<string> write)
        {
            write("\tbeq\t" + Anchor + "_F" + subLabelIndex);
            write("\tbcs\t" + Anchor);
            write(Anchor + "_F" + subLabelIndex + ":");
        }

        private void BranchLessThan(bool operandZero, Action<string> write)
        {
            if (operandZero) {
                write("\tbmi\t" + Anchor);
            }
            else {
                write("\tbvs\t" + Anchor + "_OF" + subLabelIndex);
                write("\tbmi\t" + Anchor);
                write("\tjmp\t" + Anchor + "_F" + subLabelIndex);
                write(Anchor + "_OF" + subLabelIndex + ":");
                write("\tbpl\t" + Anchor);
            }
            write(Anchor + "_F" + subLabelIndex + ":");
            ++subLabelIndex;
        }

        private void BranchLessThanOrEqualTo(bool operandZero, Action<string> write)
        {
            write("\tbeq\t" + Anchor);
            BranchLessThan(operandZero, write);
        }


        private void BranchGreaterThanOrEqualTo(bool operandZero, Action<string> write)
        {
            if (operandZero) {
                write("\tbpl\t" + Anchor);
            }
            else {
                write("\tbvs\t" + Anchor + "_OF" + subLabelIndex);
                write("\tbpl\t" + Anchor);
                write("\tjmp\t" + Anchor + "_F" + subLabelIndex);
                write(Anchor + "_OF" + subLabelIndex + ":");
                write("\tbmi\t" + Anchor);
            }
            write(Anchor + "_F" + subLabelIndex + ":");
            ++subLabelIndex;
        }

        private void BranchGreaterThan(bool operandZero, Action<string> write)
        {
            write("\tbeq\t" + Anchor + "_F" + subLabelIndex);
            BranchGreaterThanOrEqualTo(operandZero, write);
        }


        protected override void CompareWord()
        {
            var operandZero = RightOperand is IntegerOperand { IntegerValue: 0 };

            void CompareHighByte()
            {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
               {
                   ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                   ByteRegister.A.Operate(this, "cmp", false, Compiler.HighByteOperand(RightOperand));
               });
            }

            void CompareLowByte()
            {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
               {
                   ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                   ByteRegister.A.Operate(this, "cmp", false, Compiler.LowByteOperand(RightOperand));
               });
            }

            void CompareToZero()
            {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
               {
                   ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                   ByteRegister.A.Operate(this, "ora", false, Compiler.HighByteOperand(LeftOperand));
               });
            }

            void WriteInstructions(Action<bool> signedBranch, Action unsignedBranch, Action lowByteBranch)
            {
                CompareHighByte();
                if (Signed) {
                    signedBranch(operandZero);
                }
                else {
                    unsignedBranch();
                }
                WriteLine("\tbne\t" + Anchor + "_end");

                CompareLowByte();
                lowByteBranch();
                WriteLine(Anchor + "_end:");
            }

            switch (OperatorId) {
                case Keyword.Equal:
                    if (operandZero) {
                        CompareToZero();
                        WriteJumpLine("\tbeq\t" + Anchor);
                    }
                    else {
                        CompareHighByte();
                        WriteJumpLine("\tbne\t" + Anchor + "_ne" + subLabelIndex);
                        CompareLowByte();
                        WriteJumpLine("\tbeq\t" + Anchor);
                        WriteJumpLine(Anchor + "_ne" + subLabelIndex + ":");
                        ++subLabelIndex;
                    }

                    break;
                case Keyword.NotEqual:
                    if (operandZero) {
                        CompareToZero();
                        WriteJumpLine("\tbne\t" + Anchor);
                    }
                    else {
                        CompareHighByte();
                        WriteJumpLine("\tbeq\t" + Anchor + "_eq" + subLabelIndex);
                        CompareLowByte();
                        WriteJumpLine("\tbne\t" + Anchor);
                        WriteJumpLine(Anchor + "_eq" + subLabelIndex + ":");
                        ++subLabelIndex;
                    }
                    break;
                case '<':
                    WriteInstructions(
                        z => BranchLessThan(z, WriteLine),
                        () => BranchLowerThan(WriteLine),
                        () => BranchLowerThan(WriteLine));
                    break;
                case '>':
                    WriteInstructions(
                        z => BranchGreaterThan(z, WriteLine),
                        () => BranchHigherThan(WriteLine),
                        () => BranchHigherThan(WriteLine));
                    break;
                case Keyword.LessEqual:
                    WriteInstructions(
                        z => BranchLessThanOrEqualTo(z, WriteLine),
                        () => BranchLowerThan(WriteLine),
                        () => BranchLowerThanOrSameTo(WriteLine));
                    break;
                case Keyword.GreaterEqual:
                    WriteInstructions(z => BranchGreaterThanOrEqualTo(z, WriteLine),
                        () => BranchLowerThan(WriteLine),
                        () => BranchLowerThanOrSameTo(WriteLine));
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Register value is not guaranteed due to branching
            RemoveRegisterAssignment(ByteRegister.A);
        }
    }
}