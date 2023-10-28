using System;

namespace Inu.Cate.Mc6800.Mc6801
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        protected override int Threshold() => 8;

        protected override void ShiftConstant(int count)
        {
            Action action = OperatorId switch
            {
                Keyword.ShiftLeft => () =>
                {
                    WriteLine("\tasld");
                }
                ,
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => () =>
                {
                    WriteLine("\tasra");
                    WriteLine("\trorb");
                }
                ,
                Keyword.ShiftRight => () =>
                {
                    WriteLine("\tlsrd");
                }
                ,
                _ => throw new NotImplementedException()
            };

            if (
                LeftOperand.SameStorage(DestinationOperand) &&
                (!(DestinationOperand is IndirectOperand indirectOperand) || PointerRegister.X.IsOffsetInRange(indirectOperand.Offset + 1))
            ) {
                for (var i = 0; i < count; ++i) {
                    action();
                }
                return;
            }
            using (WordOperation.ReserveRegister(this, PairRegister.D)) {
                PairRegister.D.Load(this, LeftOperand);
                for (var i = 0; i < count; ++i) {
                    action();
                }
                PairRegister.D.Store(this, DestinationOperand);
            }
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            var functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftWord",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightWord"
                    : "cate.ShiftRightSignedWord",
                _ => throw new NotImplementedException()
            };
            using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                IndexRegister.X.Load(this, LeftOperand);
                ByteRegister.B.Load(this, counterOperand);
                RemoveRegisterAssignment(ByteRegister.B);
                Compiler.CallExternal(this, functionName);
                IndexRegister.X.Store(this, DestinationOperand);
            }
        }
    }
}
