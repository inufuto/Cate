using System;
using System.Collections.Generic;

namespace Inu.Cate.MuCom87
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        private static int subLabelIndex;

        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor) { }

        protected override void CompareByte()
        {
            switch (OperatorId) {
                case Keyword.Equal:
                    Operate("nea|nei");
                    return;
                case Keyword.NotEqual:
                    Operate("eqa|eqi");
                    return;
                case '<':
                    Operate("eqa|eqi", "gta|gti");
                    return;
                case '>':
                    Operate("eqa|eqi", "lta|lti");
                    return;
                case Keyword.LessEqual:
                    BranchLessEqualByte();
                    return;
                case Keyword.GreaterEqual:
                    BranchGreaterEqualByte();
                    return;
            }
            throw new NotImplementedException();
        }

        private void BranchNotEqual()
        {
            Operate("eqa|eqi");
            WriteJumpLine("\tjr\t" + Anchor);
        }


        private void BranchEqualByte()
        {
            Operate("nea|nei");
            WriteJumpLine("\tjr\t" + Anchor);
        }

        private void BranchLessEqualByte()
        {
            if (Signed) {
                CallExternalByte("cate.GreaterThanByte");
                WriteJumpLine("\tjr\t" + Anchor);
                return;
            }
            Operate("gta|gti");
            WriteJumpLine("\tjr\t" + Anchor);
        }

        private void BranchGreaterEqualByte()
        {
            if (Signed) {
                CallExternalByte("cate.LessThanByte");
                WriteJumpLine("\tjr\t" + Anchor);
                return;
            }
            Operate("lta|lti");
            WriteJumpLine("\tjr\t" + Anchor);
        }

        private void Operate(params string[] operations)
        {
            switch (RightOperand) {
                case IntegerOperand integerOperand:
                    OperateConstant(operations, integerOperand.IntegerValue.ToString());
                    return;
                case StringOperand stringOperand:
                    OperateConstant(operations, stringOperand.StringValue);
                    return;
                case ByteRegisterOperand registerOperand:
                    OperateRegister(operations, registerOperand.Register);
                    return;
                case VariableOperand variableOperand: {
                        var register = GetVariableRegister(variableOperand);
                        if (register is Cate.ByteRegister byteRegister) {
                            OperateRegister(operations, byteRegister);
                            return;
                        }
                        break;
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var register = GetVariableRegister(pointer, 0);
                        if (register is WordRegister wordRegister) {
                            OperateIndirect(operations, wordRegister);
                            return;
                        }
                        break;
                    }
            }
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, RightOperand);
                WriteLine("\tstaw\t" + ByteWorkingRegister.TemporaryByte);
            });
            OperateWorkingRegister(operations, ByteWorkingRegister.TemporaryByte);
        }

        private void OperateIndirect(string[] operations, WordRegister register)
        {
            ByteRegister.A.Load(this, LeftOperand);
            foreach (var operation in operations) {
                WriteJumpLine("\t" + operation.Split('|')[0] + "x\t" + register.HighName);
                WriteJumpLine("\tjr\t" + Anchor);
            }
        }

        private void OperateRegister(IEnumerable<string> operations, Cate.ByteRegister? register)
        {
            switch (register) {
                case ByteRegister byteRegister:
                    OperateRegister(operations, byteRegister.Name);
                    return;
                case ByteWorkingRegister workingRegister:
                    OperateWorkingRegister(operations, workingRegister.Name);
                    return;
            }
            throw new NotImplementedException();
        }

        private void OperateWorkingRegister(IEnumerable<string> operations, string name)
        {
            ByteRegister.A.Load(this, LeftOperand);
            foreach (var operation in operations) {
                WriteJumpLine("\t" + operation.Split('|')[0] + "w\t" + name);
                WriteJumpLine("\tjr\t" + Anchor);
            }
        }

        private void OperateRegister(IEnumerable<string> operations, string name)
        {
            ByteRegister.A.Load(this, LeftOperand);
            foreach (var operation in operations) {
                WriteJumpLine("\t" + operation.Split('|')[0] + "\ta," + name);
                WriteJumpLine("\tjr\t" + Anchor);
            }
        }

        private void OperateConstant(IEnumerable<string> operations, string value)
        {
            ByteRegister.A.Load(this, LeftOperand);
            foreach (var operation in operations) {
                WriteJumpLine("\t" + operation.Split('|')[1] + "\ta," + value);
                WriteJumpLine("\tjr\t" + Anchor);
            }
        }

        private void Operate(string operation, Operand rightOperand)
        {
            if (Equals(LeftOperand.Register, ByteRegister.A)) {
                ByteRegister.A.Operate(this, operation, false, rightOperand);
                return;
            }
            if (RightOperand.Register != null || RightOperand is ConstantOperand) {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, LeftOperand);
                    ByteRegister.A.Operate(this, operation, false, rightOperand);
                });
                return;
            }
            ByteOperation.UsingAnyRegister(this, ByteRegister.RegistersOtherThan(ByteRegister.A), temporaryRegister =>
            {
                temporaryRegister.Load(this, RightOperand);
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, LeftOperand);
                    ByteRegister.A.Operate(this, operation, false, rightOperand);
                });
            });
        }

        //private void Operate(string operation)
        //{
        //    if (Equals(RightOperand.Register, ByteRegister.A)) {
        //        List<Cate.ByteRegister> candidates = new List<Cate.ByteRegister>(ByteRegister.RegistersOtherThan(ByteRegister.A));
        //        ByteOperation.UsingAnyRegister(this, candidates,
        //            temporaryRegister =>
        //        {
        //            temporaryRegister.CopyFrom(this, ByteRegister.A);
        //            Operate(operation, new ByteRegisterOperand(RightOperand.Type, temporaryRegister));
        //        });
        //        return;
        //    }
        //    Operate(operation, RightOperand);
        //}

        private void CallExternalByte(string functionName)
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
            WriteLine("\tsknz");
        }


        protected override void CompareWord()
        {
            void CompareHighByte(string operation)
            {
                var registerInUse = IsRegisterInUse(ByteRegister.A);
                if (registerInUse) {
                    WriteLine("\tstaw\t" + ByteWorkingRegister.TemporaryByte);
                }
                ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, operation, false, Compiler.HighByteOperand(RightOperand));
                if (registerInUse) {
                    WriteLine("\tldaw\t" + ByteWorkingRegister.TemporaryByte);
                }
            }

            void CompareLowByte(string operation)
            {
                var registerInUse = IsRegisterInUse(ByteRegister.A);
                if (registerInUse) {
                    WriteLine("\tstaw\t" + ByteWorkingRegister.TemporaryByte);
                }
                ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, operation, false, Compiler.LowByteOperand(RightOperand));
                if (registerInUse) {
                    WriteLine("\tldaw\t" + ByteWorkingRegister.TemporaryByte);
                }
            }

            switch (OperatorId) {
                case Keyword.Equal:
                    CompareHighByte("eqa|eqi");
                    WriteJumpLine("\tjr\t" + Anchor + "_ne" + subLabelIndex);
                    CompareLowByte("nea|nei");
                    WriteJumpLine("\tjr\t" + Anchor);
                    WriteJumpLine(Anchor + "_ne" + subLabelIndex + ":");
                    ++subLabelIndex;
                    return;
                case Keyword.NotEqual:
                    CompareHighByte("nea|nei");
                    WriteJumpLine("\tjr\t" + Anchor + "_eq" + subLabelIndex);
                    CompareLowByte("eqa|eqi");
                    WriteJumpLine("\tjr\t" + Anchor);
                    WriteJumpLine(Anchor + "_eq" + subLabelIndex + ":");
                    ++subLabelIndex;
                    return;
                case '<':
                    CallExternalWord(Signed ? "cate.LessThanSignedWord" : "cate.LessThanWord");
                    return;
                case '>':
                    CallExternalWord(Signed ? "cate.GreaterThanSignedWord" : "cate.GreaterThanWord");
                    break;
                case Keyword.LessEqual:
                    CallExternalWord(Signed ? "cate.LessEqualSignedWord" : "cate.LessEqualWord");
                    return;
                case Keyword.GreaterEqual:
                    CallExternalWord(Signed ? "cate.LessEqualSignedWord" : "cate.LessEqualWord");
                    return;
            }
            throw new NotImplementedException();
        }
        private void CallExternalWord(string functionName)
        {
            WordOperation.UsingRegister(this, WordRegister.Bc, () =>
            {
                WordRegister.Bc.Load(this, RightOperand);
                WordOperation.UsingRegister(this, WordRegister.Hl, () =>
                {
                    WordRegister.Hl.Load(this, LeftOperand);
                    Compiler.CallExternal(this, functionName);
                });
            });
            WriteLine("\tsknz");
            WriteJumpLine("\tjr\t" + Anchor);
        }
    }
}
