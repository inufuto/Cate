using System;

namespace Inu.Cate.Mc6809
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        protected override void ShiftConstant(int count)
        {
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

            void ShiftPairRegister()
            {
                for (var i = 0; i < count; ++i) {
                    byteAction(operation => { WriteLine("\t" + operation + "b"); },
                        operation => { WriteLine("\t" + operation + "a"); });
                }
            }

            void ViaD()
            {
                WordOperation.UsingRegister(this, WordRegister.D, DestinationOperand, () =>
                {
                    WordRegister.D.Load(this, LeftOperand);
                    ShiftPairRegister();
                    WordRegister.D.Store(this, DestinationOperand);
                });
            }

            if (LeftOperand.SameStorage(DestinationOperand)) {
                if (Equals(DestinationOperand.Register, WordRegister.D)) {
                    ShiftPairRegister();
                    return;
                }
                if (DestinationOperand.Register != null) {
                    ViaD();
                    return;
                }
                if (!(DestinationOperand is IndirectOperand indirectOperand) || WordRegister.X.IsOffsetInRange(indirectOperand.Offset + 1)) {
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
            }

            ViaD();
            //ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            //{
            //    ByteOperation.UsingRegister(this, ByteRegister.B, () =>
            //    {
            //        WordRegister.D.Load(this, LeftOperand);
            //        ShiftPairRegister();
            //        WordRegister.D.Store(this, DestinationOperand);
            //    });
            //});
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
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                WordRegister.X.Load(this, LeftOperand);
                WriteLine("\tstx\t" + DirectPage.Word);
                ByteRegister.B.Load(this, counterOperand);
                RemoveRegisterAssignment(ByteRegister.B);
                Compiler.CallExternal(this, functionName);
                WriteLine("\tldx\t" + DirectPage.Word);
                WordRegister.X.Store(this, DestinationOperand);
            });
        }
    }
}