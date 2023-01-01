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
            if (count == -8) {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    if (DestinationOperand.Register is WordRegister destinationOperandRegister)
                    {
                        Debug.Assert(destinationOperandRegister.Low != null);
                        destinationOperandRegister.Low.LoadConstant(this, 0);
                    }
                    else {
                        ByteRegister.A.LoadConstant(this, 0);
                        ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    }
                });
                return;
            }
            if (count == 8) {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    if (DestinationOperand.Register is WordRegister destinationOperandRegister)
                    {
                        Debug.Assert(destinationOperandRegister.High != null);
                        destinationOperandRegister.High.LoadConstant(this, 0);
                    }
                    else {
                        ByteRegister.A.LoadConstant(this, 0);
                        ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    }
                });
                return;
            }
            CallExternal(() => ByteRegister.B.LoadConstant(this, count));
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
                ChangedRegisters.Add(WordRegister.Hl);
                ChangedRegisters.Add(ByteRegister.B);
            });
        }
    }
}
