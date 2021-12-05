using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Inu.Cate.MuCom87
{
    class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        {
        }

        protected override void ShiftConstant(int count)
        {
            if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
                CallExternal(() => ByteRegister.B.LoadConstant(this, count));
                return;
            }

            Action<Action<string>, Action<string>> byteAction = OperatorId switch
            {
                Keyword.ShiftLeft => (low, high) =>
                {
                    low("shal");
                    high("ral");
                }
                ,
                Keyword.ShiftRight => (low, high) =>
                {
                    high("shar");
                    low("rar");
                }
                ,
                _ => throw new NotImplementedException()
            };
            if (LeftOperand.SameStorage(DestinationOperand)) {
                for (var i = 0; i < count; ++i) {
                    byteAction(operation =>
                    {
                        ByteOperation.Operate(this, operation, true, Compiler.LowByteOperand(DestinationOperand));
                    }, operation =>
                    {
                        ByteOperation.Operate(this, operation, true, Compiler.HighByteOperand(DestinationOperand));
                    });
                }
                return;
            }
            WordOperation.UsingAnyRegister(this, WordRegister.Registers, DestinationOperand, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                for (var i = 0; i < count; ++i) {
                    byteAction(operation =>
                    {
                        Debug.Assert(temporaryRegister.Low != null);
                        temporaryRegister.Low.Operate(this, operation, true, 1);
                    }, operation =>
                    {
                        Debug.Assert(temporaryRegister.High != null);
                        temporaryRegister.High.Operate(this, operation, true, 1);
                    });
                }
                temporaryRegister.Store(this, DestinationOperand);
            });
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            CallExternal(() => ByteRegister.B.Load(this, counterOperand));
        }

        private void CallExternal(Action loadB)
        {
            string functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftHl",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedHl"
                    : "cate.ShiftRightHl",
                _ => throw new NotImplementedException()
            };
            WordOperation.UsingRegister(this, WordRegister.Hl, LeftOperand, () =>
            {
                WordRegister.Hl.Load(this, LeftOperand);
                ByteOperation.UsingRegister(this, ByteRegister.B, () =>
                {
                    loadB();
                    Compiler.CallExternal(this, functionName);
                });
                WordRegister.Hl.Store(this, DestinationOperand);
            });
        }
    }
}
