using System;

namespace Inu.Cate.Mos6502
{
    class MonomialInstruction : Cate.MonomialInstruction
    {
        public MonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand)
        { }

        public override void BuildAssembly()
        {
            if (DestinationOperand.Type.ByteCount == 1) {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, SourceOperand);
                    ByteRegister.A.Operate(this, "eor", true, "#$ff");
                    if (OperatorId == '-') {
                        ByteRegister.A.Operate(this, "clc|adc", true, "#1");
                    }
                    ByteRegister.A.Store(this, DestinationOperand);
                });
            }
            else {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    switch (OperatorId) {
                        case '~':
                            ByteRegister.A.Load(this, Compiler.LowByteOperand(SourceOperand));
                            ByteRegister.A.Operate(this, "eor", true, "#$ff");
                            ByteRegister.A.Load(this, Compiler.LowByteOperand(DestinationOperand));
                            ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                            ByteRegister.A.Operate(this, "eor", true, "#$ff");
                            ByteRegister.A.Load(this, Compiler.HighByteOperand(DestinationOperand));
                            break;
                        case '-':
                            ByteRegister.A.Load(this, Compiler.LowByteOperand(SourceOperand));
                            ByteRegister.A.Operate(this, "eor", true, "#$ff");
                            ByteRegister.A.Operate(this, "clc|adc", true, "#1");
                            ByteRegister.A.Load(this, Compiler.LowByteOperand(DestinationOperand));
                            ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                            ByteRegister.A.Operate(this, "eor", true, "#$ff");
                            ByteRegister.A.Operate(this, "adc", true, "#0");
                            ByteRegister.A.Load(this, Compiler.HighByteOperand(DestinationOperand));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                });
            }
        }
    }
}