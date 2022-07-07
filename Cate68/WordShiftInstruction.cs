using System;

namespace Inu.Cate.Mc6800
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        protected override void ShiftConstant(int count)
        {
            if (count > 2)
            {
                ShiftVariable(RightOperand);
                return;
            }

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

            if (
                LeftOperand.SameStorage(DestinationOperand) &&
                (!(DestinationOperand is IndirectOperand indirectOperand) || WordRegister.X.IsOffsetInRange(indirectOperand.Offset + 1))
            ) {
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
            ByteRegister.UsingPair(this, () =>
            {
                ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                ByteRegister.B.Load(this, Compiler.LowByteOperand(LeftOperand));
                for (var i = 0; i < count; ++i) {
                    byteAction(operation =>
                    {
                        WriteLine("\t" + operation + "b");
                    }, operation =>
                    {
                        WriteLine("\t" + operation + "a");
                    });
                }
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                ByteRegister.B.Store(this, Compiler.HighByteOperand(LeftOperand));
            });
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            string functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftWord",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightWord"
                    : "cate.ShiftRightSignedWord",
                _ => throw new NotImplementedException()
            };
            ByteRegister.Using(this, ByteRegister.B, () =>
            {
                WordRegister.X.Load(this, LeftOperand);
                ByteRegister.B.Load(this, counterOperand);
                RemoveRegisterAssignment(ByteRegister.B);
                Compiler.CallExternal(this, functionName);
                WordRegister.X.Store(this, DestinationOperand);
            });
        }
    }
}