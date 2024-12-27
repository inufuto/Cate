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
            if (
                LeftOperand.SameStorage(DestinationOperand) && count <= 2 &&
                (!(DestinationOperand is IndirectOperand indirectOperand) || IndexRegister.X.IsOffsetInRange(indirectOperand.Offset + 1))
            ) {
                Action<Action<string>, Action<string>> byteAction = OperatorId switch
                {
                    Keyword.ShiftLeft => (low, high) =>
                    {
                        low("asl");
                        high("rol");
                    }
                    ,
                    Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => (low, high) =>
                    {
                        high("\tasr");
                        low("\tror");
                    }
                    ,
                    Keyword.ShiftRight => (low, high) =>
                    {
                        high("\tlsr");
                        low("\tror");
                    }
                    ,
                    _ => throw new NotImplementedException()
                };
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
                using (WordOperation.ReserveRegister(this, PairRegister.D)) {
                    PairRegister.D.Load(this, LeftOperand);
                    for (var i = 0; i < count; ++i) {
                        action();
                    }
                    PairRegister.D.Store(this, DestinationOperand);
                }
            }
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            var functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftWord",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedWord"
                    : "cate.ShiftRightWord",
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
