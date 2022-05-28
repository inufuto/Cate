using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Cate.I8086
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                WordOperation.UsingAnyRegister(this, WordRegister.Registers, DestinationOperand, null, register =>
                 {
                     register.LoadConstant(this, 0);
                     register.Store(this, DestinationOperand);
                 });
                return;
            }
            if (LeftOperand.Type.ByteCount == 1) {
                ByteOperation.UsingRegister(this, ByteRegister.Ah, () =>
                {
                    ByteRegister.Al.Load(this, LeftOperand);
                    WriteLine("\tmov ah," + RightValue);
                    WriteLine("\tmul ah");
                    ChangedRegisters.Add(ByteRegister.Ah);
                    if (DestinationOperand.Type.ByteCount == 1) {
                        ByteRegister.Al.Store(this, DestinationOperand);
                        ChangedRegisters.Add(ByteRegister.Al);
                    }
                    else {
                        WordRegister.Ax.Store(this, DestinationOperand);
                        ChangedRegisters.Add(WordRegister.Ax);
                    }
                });
                return;
            }
            WordOperation.UsingRegister(this, WordRegister.Dx, () =>
            {
                WordRegister.Ax.Load(this, LeftOperand);
                WriteLine("\tmov dx," + RightValue);
                WriteLine("\tmul dx");
                ChangedRegisters.Add(WordRegister.Dx);
                if (DestinationOperand.Type.ByteCount == 1) {
                    ByteRegister.Al.Store(this, DestinationOperand);
                    ChangedRegisters.Add(ByteRegister.Al);
                }
                else {
                    WordRegister.Ax.Store(this, DestinationOperand);
                    ChangedRegisters.Add(WordRegister.Ax);
                }
            });
        }
    }
}
