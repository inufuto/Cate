﻿using System;
using static System.String;

namespace Inu.Cate.MuCom87;

internal abstract class CompareInstruction(
    Function function,
    int operatorId,
    Operand leftOperand,
    Operand rightOperand,
    Anchor anchor)
    : Cate.CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor)
{
    private static int subLabelIndex;

    protected override void CompareByte()
    {
        string? label = null;

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
        return;

        void WriteNot()
        {
            label = Anchor + "_ne" + subLabelIndex;
            WriteJumpLine("\tjr\t" + Anchor + "_ne" + subLabelIndex);
            WriteJumpLine("\tjr\t" + Anchor);
        }

        void Write()
        {
            WriteJumpLine("\tjr\t" + Anchor);
        }
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
                if (register is WordRegister pointerRegister) {
                    OperateIndirect(operation, action, pointerRegister);
                    return;
                }
                break;
            }
        }

        OperateViaAccumulator(operation, action);
    }

    protected abstract void OperateViaAccumulator(string operation, Action action);

    private void OperateIndirect(string operation, Action action, WordRegister pointerRegister)
    {
        ByteRegister.A.Load(this, LeftOperand);
        WriteJumpLine("\t" + operation.Split('|')[0] + "x\t" + pointerRegister.AsmName);
        action();
    }

    private void OperateRegister(string operation, Action action, Cate.ByteRegister? register)
    {
        if (register is ByteRegister byteRegister) {
            OperateRegister(operation, action, byteRegister.AsmName);
            return;
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
        using (ByteOperation.ReserveRegister(this, ByteRegister.C)) {
            ByteRegister.C.Load(this, RightOperand);
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, LeftOperand);
                Compiler.CallExternal(this, functionName);
            }
        }
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
        using (WordOperation.ReserveRegister(this, WordRegister.Hl)) {
            using (WordOperation.ReserveRegister(this, WordRegister.Bc)) {
                WordRegister.Bc.Load(this, RightOperand);
                WordRegister.Hl.Load(this, LeftOperand);
                Compiler.CallExternal(this, functionName);
            }
            AddChanged(WordRegister.Hl);
        }

        if (skip.Equals("skz")) {
            ((Compiler)Compiler).SkipIfZero(this);
        }
        else {
            WriteJumpLine("\t" + skip);
        }
        WriteJumpLine("\tjr\t" + Anchor);
    }
}