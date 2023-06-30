namespace Inu.Cate.Sc62015
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftConstant(int count)
        {
            if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
                CallExternal(() => ByteInternalRam.CL.LoadConstant(this, count));
                return;
            }

            Action<Action<string>, Action<string>> byteAction = OperatorId switch
            {
                Keyword.ShiftLeft => (low, high) =>
                {
                    low("shl");
                    high("rol");
                }
                ,
                Keyword.ShiftRight => (low, high) =>
                {
                    high("\tshr");
                    low("\tror");
                }
                ,
                _ => throw new NotImplementedException()
            };
            if (LeftOperand.SameStorage(DestinationOperand)) {
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
                void ViaRegister(WordInternalRam internalRam)
                {
                    internalRam.Load(this, LeftOperand);
                    for (var i = 0; i < count; ++i) {
                        byteAction(operation =>
                        {
                            WriteLine("\t" + operation + " (" + internalRam.Label + ")");
                        }, operation =>
                        {
                            WriteLine("\t" + operation + " (" + internalRam.Label + "+1)");
                        });
                    }
                    internalRam.Store(this, DestinationOperand);
                }
                {
                    if (DestinationOperand.Register is WordInternalRam wordRegister && !RightOperand.Conflicts(wordRegister)) {
                        ViaRegister(wordRegister);
                        return;
                    }

                    using var reservation = WordOperation.ReserveAnyRegister(this, WordInternalRam.Registers, LeftOperand);
                    ViaRegister((WordInternalRam)reservation.WordRegister);
                    reservation.WordRegister.Store(this, DestinationOperand);
                }
            }
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            CallExternal(() => ByteInternalRam.CL.Load(this, counterOperand));
        }

        private void CallExternal(Action load)
        {
            var functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftWord",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedWord"
                    : "cate.ShiftRightWord",
                _ => throw new NotImplementedException()
            };
            {
                void ViaRegister(WordInternalRam internalRam)
                {
                    internalRam.Load(this, LeftOperand);
                    WriteLine("\tmv px," + internalRam.Label);
                    using (ByteOperation.ReserveRegister(this, ByteInternalRam.CL)) {
                        load();
                        Compiler.CallExternal(this, functionName);
                    }
                }

                if (DestinationOperand.Register is WordInternalRam wordRegister && !RightOperand.Conflicts(wordRegister)) {
                    ViaRegister(wordRegister);
                    return;
                }
                using var reservation =
                    WordOperation.ReserveAnyRegister(this, WordInternalRam.Registers, LeftOperand);
                ViaRegister((WordInternalRam)reservation.WordRegister);
                reservation.WordRegister.Store(this, DestinationOperand);
            }
        }
    }
}
