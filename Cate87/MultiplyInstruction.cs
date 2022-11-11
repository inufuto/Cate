using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Inu.Cate.MuCom87
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        {
        }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                if (DestinationOperand.Register is WordRegister destinationRegister) {
                    destinationRegister.LoadConstant(this, 0);
                    return;
                }
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.LoadConstant(this, 0);
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                });
                return;
            }
            if (RightValue == 1) {
                if (DestinationOperand.Register is WordRegister destinationRegister && !Equals(destinationRegister, LeftOperand.Register)) {
                    destinationRegister.Load(this, LeftOperand);
                    return;
                }
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                });
                return;
            }

            void Call()
            {
                WordRegister.Hl.Load(this, LeftOperand);
                ChangedRegisters.Add(WordRegister.Hl);
                RemoveRegisterAssignment(WordRegister.Hl);
                ByteOperation.UsingRegister(this, ByteRegister.C, () =>
                {
                    ByteRegister.C.LoadConstant(this, RightValue);
                    Compiler.CallExternal(this, "cate.MultiplyHlC");
                });
                WordRegister.Hl.Store(this, DestinationOperand);
            }

            if (Equals(DestinationOperand.Register, WordRegister.Hl)) {
                Call();
                return;
            }
            WordOperation.UsingRegister(this, WordRegister.Hl, Call);
        }
    }
}

