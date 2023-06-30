namespace Inu.Cate.Sc62015
{
    internal class ByteShiftInstruction : Cate.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftConstant(int count)
        {
            if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
                using (ByteOperation.ReserveRegister(this, ByteInternalRam.CL)) {
                    ByteInternalRam.CL.LoadConstant(this, count);
                    CallExternal("cate.ShiftRightSignedA");
                }
                return;
            }
            base.ShiftConstant(count);
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            var functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftA",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedA"
                    : "cate.ShiftRightA",
                _ => throw new NotImplementedException()
            };
            using (ByteOperation.ReserveRegister(this, ByteInternalRam.CL)) {
                ByteInternalRam.CL.Load(this, RightOperand);
                CallExternal(functionName);
            }
        }

        private void CallExternal(string functionName)
        {
            void Call()
            {
                ByteRegister.A.Load(this, LeftOperand);
                Compiler.CallExternal(this, functionName);
                RemoveRegisterAssignment(ByteRegister.A);
                AddChanged(ByteRegister.A);
                RemoveRegisterAssignment(ByteInternalRam.CL);
                AddChanged(ByteInternalRam.CL);
                ByteRegister.A.Store(this, DestinationOperand);
            }

            if (Equals(DestinationOperand.Register, ByteRegister.A)) {
                Call();
                return;
            }
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                Call();
            }
        }

        protected override string Operation()
        {
            return OperatorId switch
            {
                Keyword.ShiftLeft => "shl",
                Keyword.ShiftRight when !((IntegerType)LeftOperand.Type).Signed => "shr",
                _ => throw new NotImplementedException()
            };
        }
    }
}
