namespace Inu.Cate.Sc62015
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        private static int subLabelIndex;

        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor) { }

        private Register? LeftRegister()
        {
            if (LeftOperand is VariableOperand variableOperand) {
                return GetVariableRegister(variableOperand);
            }
            return null;
        }

        protected override void CompareByte()
        {
            void CompareConstant(Register register, string rightValue)
            {
                WriteLine("\tcmp " + register.AsmName + "," + rightValue);
            }

            void Compare()
            {
                if (RightOperand is ConstantOperand rightConstantOperand) {
                    var rightValue = rightConstantOperand.MemoryAddress();

                    if (LeftOperand is VariableOperand leftVariableOperand) {
                        var leftRegister = GetVariableRegister(leftVariableOperand);
                        if (leftRegister != null) {
                            if (Equals(leftRegister, ByteRegister.A) || leftRegister is ByteInternalRam) {
                                CompareConstant((ByteRegister)leftRegister, rightValue);
                            }
                            else {
                                using var reservation = ByteOperation.ReserveAnyRegister(this, ByteRegister.AccumulatorAndInternalRam, LeftOperand);
                                var temporaryRegister = reservation.ByteRegister;
                                temporaryRegister.Load(this, LeftOperand);
                                CompareConstant(temporaryRegister, rightValue);
                            }
                        }
                        else {
                            WriteLine("\tcmp [" + leftVariableOperand.MemoryAddress() + "]," + rightValue);
                        }
                    }
                    else {
                        using var reservation = ByteOperation.ReserveAnyRegister(this, ByteRegister.AccumulatorAndInternalRam, LeftOperand);
                        var leftRegister = reservation.ByteRegister;
                        leftRegister.Load(this, LeftOperand);
                        CompareConstant((ByteRegister)leftRegister, rightValue);
                    }
                }
                else {
                    using var leftReservation = ByteOperation.ReserveAnyRegister(this, ByteInternalRam.Registers, LeftOperand);
                    var leftRegister = leftReservation.ByteRegister;
                    leftRegister.Load(this, LeftOperand);
                    using var rightReservation = ByteOperation.ReserveAnyRegister(this, ByteRegister.AccumulatorAndInternalRam, RightOperand);
                    var rightRegister = rightReservation.ByteRegister;
                    rightRegister.Load(this, RightOperand);
                    WriteLine("\tcmp " + leftRegister.AsmName + "," + rightRegister.AsmName);
                }
            }

            void CompareZero()
            {
                if (CanOmitOperation(Flag.Z)) return;
                using var reservation = ByteOperation.ReserveAnyRegister(this, ByteInternalRam.Registers, LeftOperand);
                var leftRegister = reservation.ByteRegister;
                leftRegister.Load(this, LeftOperand);
                WriteLine("\tor " + leftRegister.AsmName + "," + leftRegister.AsmName);
            }

            void CompareSigned()
            {
                using (ByteOperation.ReserveRegister(this, ByteInternalRam.DH)) {
                    ByteInternalRam.DH.Load(this, RightOperand);
                    using (ByteOperation.ReserveRegister(this, ByteInternalRam.DL)) {
                        ByteInternalRam.DL.Load(this, LeftOperand);
                        Compiler.CallExternal(this, "cate.CompareSignedByte");
                    }
                }
            }

            switch (OperatorId) {
                case Keyword.Equal:
                    if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
                        if (CanOmitOperation(Flag.Z)) goto JumpEqual;
                        if (!Equals(LeftRegister(), ByteRegister.A))
                            CompareZero();
                        else
                            Compare();
                    }
                    else
                        Compare();
                    JumpEqual:
                    JumpEqual();
                    break;
                case Keyword.NotEqual:
                    if (RightOperand is IntegerOperand { IntegerValue: 0 }) {
                        if (CanOmitOperation(Flag.Z)) goto jumpNotEqual;
                        if (!Equals(LeftRegister(), ByteRegister.A))
                            CompareZero();
                        else
                            Compare();
                    }
                    else
                        Compare();
                    jumpNotEqual:
                    JumpNotEqual();
                    break;
                case '<':
                    if (Signed)
                        CompareSigned();
                    else
                        Compare();
                    JumpLess();
                    break;
                case '>':
                    if (Signed) {
                        CompareSigned();
                    }
                    else {
                        Compare();
                    }
                    JumpGreater();
                    break;
                case Keyword.LessEqual:
                    if (Signed) {
                        CompareSigned();
                    }
                    else {
                        Compare();
                    }
                    JumpLessEqual();
                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        CompareSigned();
                    }
                    else {
                        Compare();
                    }
                    JumpGreaterEqual();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override void CompareWord()
        {
            void Compare()
            {
                void CompareL(Register leftRegister)
                {
                    void CompareR(Register rightRegister)
                    {
                        WriteLine("\tcmpw " + leftRegister.AsmName + "," + rightRegister.AsmName);
                    }

                    if (RightOperand is VariableOperand rightVariableOperand) {
                        var rightRegister = GetVariableRegister(rightVariableOperand);
                        if (rightRegister != null) {
                            CompareR(rightRegister);
                        }
                        else {
                            using var rightReservation = WordOperation.ReserveAnyRegister(this, WordOperation.Registers, RightOperand);
                            rightReservation.WordRegister.Load(this, RightOperand);
                            CompareR(rightReservation.WordRegister);
                        }
                    }
                }

                if (LeftOperand is VariableOperand leftVariableOperand) {
                    var leftRegister = GetVariableRegister(leftVariableOperand);
                    if (leftRegister is WordInternalRam wordInternalRam) {
                        using (WordOperation.ReserveRegister(this, wordInternalRam)) {
                            CompareL(wordInternalRam);
                        }
                    }
                    else {
                        using var leftReservation = WordOperation.ReserveAnyRegister(this, WordInternalRam.Registers, LeftOperand);
                        leftReservation.WordRegister.Load(this, LeftOperand);
                        CompareL(leftReservation.WordRegister);
                    }
                }
            }

            void CompareSigned()
            {
                using (WordOperation.ReserveRegister(this, WordInternalRam.CX)) {
                    WordInternalRam.CX.Load(this, RightOperand);
                    using (WordOperation.ReserveRegister(this, WordInternalRam.DX)) {
                        WordInternalRam.DX.Load(this, LeftOperand);
                        Compiler.CallExternal(this, "cate.CompareSignedWord");
                    }
                }
            }

            switch (OperatorId) {
                case Keyword.Equal:
                    Compare();
                    JumpEqual();
                    break;
                case Keyword.NotEqual:
                    Compare();
                    JumpNotEqual();
                    break;
                case '<':
                    if (Signed)
                        CompareSigned();
                    else
                        Compare();
                    JumpLess();
                    break;
                case '>':
                    if (Signed) {
                        CompareSigned();
                    }
                    else {
                        Compare();
                    }
                    JumpGreater();
                    break;
                case Keyword.LessEqual:
                    if (Signed) {
                        CompareSigned();
                    }
                    else {
                        Compare();
                    }
                    JumpLessEqual();
                    break;
                case Keyword.GreaterEqual:
                    if (Signed) {
                        CompareSigned();
                    }
                    else {
                        Compare();
                    }
                    JumpGreaterEqual();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override void ComparePointer()
        {
            void Compare()
            {
                void CompareL(Register leftRegister)
                {
                    void CompareR(Register rightRegister)
                    {
                        WriteLine("\tcmpp " + leftRegister.AsmName + "," + rightRegister.AsmName);
                    }

                    if (RightOperand is VariableOperand rightVariableOperand) {
                        var rightRegister = GetVariableRegister(rightVariableOperand);
                        if (rightRegister != null) {
                            CompareR(rightRegister);
                        }
                        else {
                            using var rightReservation = PointerOperation.ReserveAnyRegister(this, PointerOperation.Registers, RightOperand);
                            rightReservation.PointerRegister.Load(this, RightOperand);
                            CompareR(rightReservation.PointerRegister);
                        }
                    }
                }

                if (LeftOperand is VariableOperand leftVariableOperand) {
                    var leftRegister = GetVariableRegister(leftVariableOperand);
                    if (leftRegister is PointerInternalRam leftInternalRam) {
                        CompareL(leftInternalRam);
                    }
                    else {
                        using var leftReservation = PointerOperation.ReserveAnyRegister(this, PointerInternalRam.Registers, LeftOperand);
                        leftReservation.PointerRegister.Load(this, LeftOperand);
                        CompareL(leftReservation.PointerRegister);
                    }
                }
            }

            switch (OperatorId) {
                case Keyword.Equal:
                    Compare();
                    JumpEqual();
                    break;
                case Keyword.NotEqual:
                    Compare();
                    JumpNotEqual();
                    break;
                case '<':
                    Compare();
                    JumpLess();
                    break;
                case '>':
                    Compare();
                    JumpGreater();
                    break;
                case Keyword.LessEqual:
                    Compare();
                    JumpLessEqual();
                    break;
                case Keyword.GreaterEqual:
                    Compare();
                    JumpGreaterEqual();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }


        private void JumpGreaterEqual()
        {
            WriteJumpLine("\tjrnc " + Anchor);
        }

        private void JumpLessEqual()
        {
            WriteJumpLine("\tjrz " + Anchor);
            WriteJumpLine("\tjrc " + Anchor);
        }

        private void JumpGreater()
        {
            WriteJumpLine("\tjrz " + Anchor + "_F" + subLabelIndex);
            WriteJumpLine("\tjrnc " + Anchor);
            WriteJumpLine(Anchor + "_F" + subLabelIndex + ":");
            ++subLabelIndex;
        }

        private void JumpLess()
        {
            WriteJumpLine("\tjrc " + Anchor);
        }

        private void JumpNotEqual()
        {
            WriteJumpLine("\tjrnz " + Anchor);
        }

        private void JumpEqual()
        {
            WriteJumpLine("\tjrz " + Anchor);
        }
    }
}
