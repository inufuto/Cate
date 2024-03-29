﻿using System;

namespace Inu.Cate.MuCom87
{
    class MonomialInstruction : Cate.MonomialInstruction
    {
        public MonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (DestinationOperand.Type.ByteCount == 1) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    ByteRegister.A.Load(this, SourceOperand);
                    ByteRegister.A.Operate(this, "xri\t", true, "$ff");
                    if (OperatorId == '-') {
                        ByteRegister.A.Operate(this, "adi\t", true, "1");
                    }
                    ByteRegister.A.Store(this, DestinationOperand);
                }
            }
            else {
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    switch (OperatorId) {
                        case '~':
                            ByteRegister.A.Load(this, Compiler.LowByteOperand(SourceOperand));
                            ByteRegister.A.Operate(this, "xri\t", true, "$ff");
                            ByteRegister.A.Load(this, Compiler.LowByteOperand(DestinationOperand));
                            ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                            ByteRegister.A.Operate(this, "xri\t", true, "$ff");
                            ByteRegister.A.Load(this, Compiler.HighByteOperand(DestinationOperand));
                            break;
                        case '-':
                            ByteRegister.A.Load(this, Compiler.LowByteOperand(SourceOperand));
                            ByteRegister.A.Operate(this, "xri\t", true, "$ff");
                            ByteRegister.A.Operate(this, "adi\t", true, "1");
                            ByteRegister.A.Load(this, Compiler.LowByteOperand(DestinationOperand));
                            ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                            ByteRegister.A.Operate(this, "xri\t", true, "$ff");
                            ByteRegister.A.Operate(this, "aci\t", true, "0");
                            ByteRegister.A.Load(this, Compiler.HighByteOperand(DestinationOperand));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
