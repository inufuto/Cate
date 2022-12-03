using System;

namespace Inu.Cate.Mc6800
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor)
            : base(function, operatorId, leftOperand, rightOperand, anchor) { }

        protected override void CompareByte()
        {
            if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
                if (OperatorId == Keyword.Equal || OperatorId == Keyword.NotEqual) {
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

            ByteRegister.UsingAny(this, LeftOperand, register =>
            {
                register.Load(this, LeftOperand);
                register.Operate(this, "cmp", false, RightOperand);
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

        protected override void CompareWord()
        {
            switch (OperatorId) {
                case Keyword.Equal:
                    WordRegister.X.Load(this, LeftOperand);
                    WordRegister.X.Operate(this, "cp", false, RightOperand);
                    WriteJumpLine("\tbeq\t" + Anchor);
                    return;
                case Keyword.NotEqual:
                    WordRegister.X.Load(this, LeftOperand);
                    WordRegister.X.Operate(this, "cp", false, RightOperand);
                    WriteJumpLine("\tbne\t" + Anchor);
                    return;
            }
            ByteOperation.UsingAnyRegister(this, register =>
            {
                void WriteInstructions(string signedBranch, string unsignedBranch, string lowByteBranch)
                {
                    register.Load(this, Compiler.HighByteOperand(LeftOperand));
                    register.Operate(this, "cmp", false, Compiler.HighByteOperand(RightOperand));
                    if (Signed) {
                        WriteLine("\t" + signedBranch + "\t" + Anchor);
                    }
                    else {
                        WriteLine("\t" + unsignedBranch + "\t" + Anchor);
                    }
                    WriteLine("\tbne\t" + Anchor + "_end");

                    register.Load(this, Compiler.LowByteOperand(LeftOperand));
                    register.Operate(this, "cmp", false, Compiler.LowByteOperand(RightOperand));
                    WriteLine("\t" + lowByteBranch + "\t" + Anchor);
                    WriteLine(Anchor + "_end:");
                }

                switch (OperatorId) {
                    case '<':
                        WriteInstructions("blt", "bcs", "bcs");
                        break;
                    case '>':
                        WriteInstructions("bgt", "bhi", "bhi");
                        break;
                    case Keyword.LessEqual:
                        WriteInstructions("ble", "bcs", "bls");
                        break;
                    case Keyword.GreaterEqual:
                        WriteInstructions("bge", "bhi", "bcc");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                // Register value is not guaranteed due to branching
                RemoveRegisterAssignment(register);
            });
        }
    }
}