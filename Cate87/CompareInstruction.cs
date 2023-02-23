using System;
using System.Collections.Generic;
using static System.String;

namespace Inu.Cate.MuCom87
{
    internal abstract class CompareInstruction : Cate.CompareInstruction
    {
        private static int subLabelIndex;

        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor) { }

        protected override void CompareByte()
        {
            string? label = null;
            void Write()
            {
                WriteJumpLine("\tjr\t" + Anchor);
            }
            void WriteNot()
            {
                label = Anchor + "_ne" + subLabelIndex;
                WriteJumpLine("\tjr\t" + Anchor + "_ne" + subLabelIndex);
                WriteJumpLine("\tjr\t" + Anchor);
            }

            switch (OperatorId) {
                case Keyword.Equal:
                    Operate("nea|nei", Write);
                    break;
                case Keyword.NotEqual:
                    Operate("eqa|eqi", Write);
                    break;
                case '<':
                    if (Signed) {
                        CallExternalByte("cate.LessThanByte", "sknz");
                    }
                    else {
                        Operate("lta|lti", WriteNot);
                    }
                    break;
                case '>':
                    if (Signed) {
                        CallExternalByte("cate.GreaterThanByte", "sknz");
                    }
                    else {
                        Operate("gta|gti", WriteNot);
                    }
                    break;
                case Keyword.LessEqual:
                    if (Signed) {
                        CallExternalByte("cate.GreaterThanByte", "skz");
                    }
                    else {
                        Operate("gta|gti", Write);
                    }
                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        CallExternalByte("cate.LessThanByte", "skz");
                    }
                    else {
                        Operate("lta|lti", Write);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (IsNullOrEmpty(label)) return;
            WriteJumpLine("\t" + label + ":");
            ++subLabelIndex;
        }


        private void Operate(string operation, Action action)
        {
            switch (RightOperand) {
                case IntegerOperand integerOperand:
                    OperateConstant(operation, action, integerOperand.IntegerValue.ToString());
                    return;
                case StringOperand stringOperand:
                    OperateConstant(operation, action, stringOperand.StringValue);
                    return;
                case ByteRegisterOperand registerOperand:
                    OperateRegister(operation, action, registerOperand.Register);
                    return;
                case VariableOperand variableOperand: {
                        var register = GetVariableRegister(variableOperand);
                        if (register is Cate.ByteRegister byteRegister && !Equals(byteRegister, ByteRegister.A)) {
                            OperateRegister(operation, action, byteRegister);
                            return;
                        }

                        break;
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        var register = GetVariableRegister(pointer, offset);
                        if (register is WordRegister wordRegister) {
                            OperateIndirect(operation, action, wordRegister);
                            return;
                        }
                        break;
                    }
            }

            OperateViaAccumulator(operation, action);
        }

        protected abstract void OperateViaAccumulator(string operation, Action action);

        private void OperateIndirect(string operation, Action action, WordRegister register)
        {
            ByteRegister.A.Load(this, LeftOperand);
            WriteJumpLine("\t" + operation.Split('|')[0] + "x\t" + register.HighName);
            action();
        }

        private void OperateRegister(string operation, Action action, Cate.ByteRegister? register)
        {
            switch (register) {
                case ByteRegister byteRegister:
                    OperateRegister(operation, action, byteRegister.Name);
                    return;
                    //case ByteWorkingRegister workingRegister:
                    //    OperateWorkingRegister(operation, action, workingRegister.Name);
                    //    return;
            }
            throw new NotImplementedException();
        }


        private void OperateRegister(string operation, Action action, string name)
        {
            ByteRegister.A.Load(this, LeftOperand);
            WriteJumpLine("\t" + operation.Split('|')[0] + "\ta," + name);
            action();
        }

        private void OperateConstant(string operation, Action action, string value)
        {
            ByteRegister.A.Load(this, LeftOperand);
            WriteJumpLine("\t" + operation.Split('|')[1] + "\ta," + value);
            action();
        }

        private void CallExternalByte(string functionName, string skip)
        {
            ByteOperation.UsingRegister(this, ByteRegister.C, () =>
            {
                ByteRegister.C.Load(this, RightOperand);
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, LeftOperand);
                    Compiler.CallExternal(this, functionName);
                });
            });
            if (skip.Equals("skz")) {
                ((Compiler)Compiler).SkipIfZero(this);
            }
            else {
                WriteJumpLine("\t" + skip);
            }
            WriteJumpLine("\tjr\t" + Anchor);
        }


        protected override void CompareWord()
        {
            switch (OperatorId) {
                case Keyword.Equal:
                    CallExternalWord("cate.EqualWord", "sknz");
                    return;
                case Keyword.NotEqual:
                    CallExternalWord("cate.EqualWord", "skz");
                    return;
                case '<':
                    CallExternalWord(Signed ? "cate.LessThanSignedWord" : "cate.LessThanWord", "sknz");
                    return;
                case '>':
                    CallExternalWord(Signed ? "cate.GreaterThanSignedWord" : "cate.GreaterThanWord", "sknz");
                    return;
                case Keyword.LessEqual:
                    CallExternalWord(Signed ? "cate.GreaterThanSignedWord" : "cate.GreaterThanWord", "skz");
                    return;
                case Keyword.GreaterEqual:
                    CallExternalWord(Signed ? "cate.LessThanSignedWord" : "cate.LessThanWord", "skz");
                    return;
            }
            throw new NotImplementedException();
        }
        private void CallExternalWord(string functionName, string skip)
        {
            WordOperation.UsingRegister(this, WordRegister.Hl, () =>
            {
                WordOperation.UsingRegister(this, WordRegister.Bc, () =>
                {
                    WordRegister.Bc.Load(this, RightOperand);
                    WordRegister.Hl.Load(this, LeftOperand);
                    Compiler.CallExternal(this, functionName);
                });
                ChangedRegisters.Add(WordRegister.Hl);
            });
            if (skip.Equals("skz")) {
                ((Compiler)Compiler).SkipIfZero(this);
            }
            else {
                WriteJumpLine("\t" + skip);
            }
            WriteJumpLine("\tjr\t" + Anchor);
        }
    }
}
