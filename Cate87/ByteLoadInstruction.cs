﻿namespace Inu.Cate.MuCom87
{
    internal class ByteLoadInstruction : Cate.ByteLoadInstruction
    {
        public ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (
                DestinationOperand.SameStorage(SourceOperand) &&
                DestinationOperand.Type.ByteCount == SourceOperand.Type.ByteCount
            ) return;
            if (DestinationOperand.Register is ByteRegister destinationRegister && SourceOperand is ConstantOperand) {
                destinationRegister.Load(this, SourceOperand);
                return;
            }
            if (Equals(SourceOperand.Register, ByteRegister.A)) {
                ByteRegister.A.Store(this, DestinationOperand);
                return;
            }

            if (SourceOperand.Register is ByteRegister byteRegister && DestinationOperand is VariableOperand { Register: null } variableOperand) {
                byteRegister.StoreToMemory(this, variableOperand.Variable, variableOperand.Offset);
                return;
            }

            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, SourceOperand);
                ByteRegister.A.Store(this, DestinationOperand);
            }
            //base.BuildAssembly();
        }
    }
}
