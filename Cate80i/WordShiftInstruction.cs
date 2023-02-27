using System;

namespace Inu.Cate.I8080
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftConstant(int count)
        {
            if (
                (OperatorId == Keyword.ShiftLeft && count == 8) ||
                (OperatorId == Keyword.ShiftRight && count == -8)
            ) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    ByteRegister.A.LoadConstant(this, 0);
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                }
                return;
            }
            if (
                (OperatorId == Keyword.ShiftLeft && count == -8) ||
                (OperatorId == Keyword.ShiftRight && count == 8)
            ) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    ByteRegister.A.LoadConstant(this, 0);
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                }
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
            using (WordOperation.ReserveRegister(this, WordRegister.Hl, LeftOperand)) {
                WordRegister.Hl.Load(this, LeftOperand);
                using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                    loadB();
                    Compiler.CallExternal(this, functionName);
                }
                WordRegister.Hl.Store(this, DestinationOperand);
                AddChanged(WordRegister.Hl);
                AddChanged(ByteRegister.B);
            }
        }
    }
}
