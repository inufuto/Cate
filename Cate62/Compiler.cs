namespace Inu.Cate.Sc62015
{

    public class Compiler : Cate.Compiler
    {
        public const string MemPageName = "@MemPage";
        private static int registerId = 0;

        public static string OffsetToString(int offset)
        {
            return offset switch
            {
                > 0 => "+" + offset,
                < 0 => "-" + (-offset),
                _ => ""
            };
        }

        public static void MakePointer(Instruction instruction, Cate.WordRegister pointerRegister)
        {
            instruction.WriteLine("\tmv x," + pointerRegister.Name);
            instruction.WriteLine("\tadd x,y");
        }

        public static int NewRegisterId()
        {
            return ++registerId;
        }

        public Compiler() : base(new ByteOperation(), new WordOperation()) { }

        public override ISet<Register> SavingRegisters(Register register)
        {
            return new HashSet<Register>() { register };
        }

        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            var ordered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null })
                .OrderBy(v => v.Range)
                .ThenByDescending(v => v.Usages.Count).ToList();
            ;
            foreach (var variable in ordered) {
                var variableType = variable.Type;
                Register? register = null;
                if (variableType.ByteCount == 1) {
                    var registers = new List<Cate.ByteRegister>();
                    registers.AddRange(ByteRegister.Registers);
                    registers.AddRange(ByteInternalRam.Registers);
                    register = AllocatableRegister(variable, registers);
                }
                else {
                    var registers = new List<Cate.WordRegister>();
                    registers.AddRange(WordRegister.Registers);
                    registers.AddRange(WordInternalRam.Registers);
                    register = AllocatableRegister(variable, registers);
                }
                if (register != null) {
                    variable.Register = register;
                }
            }
        }

        private static Register? AllocatableRegister<T>(Variable variable, IEnumerable<T> registers) where T : Register
        {
            return (from register in registers let conflict = Conflict(variable.Intersections, register) where !conflict select register).FirstOrDefault();
        }

        private static bool Conflict<T>(IEnumerable<Variable> variables, T register) where T : Register
        {
            return variables.Any(v =>
                v.Register != null && register.Conflicts(v.Register));
        }

        public override Register? ParameterRegister(int index, ParameterizableType type)
        {
            return index switch
            {
                0 => type.ByteCount switch
                {
                    1 => ByteRegister.A,
                    2 => WordRegister.BA,
                    _ => throw new ArgumentOutOfRangeException()
                },
                1 => type.ByteCount switch
                {
                    1 => ByteRegister.IL,
                    2 => WordRegister.I,
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => null
            };
        }

        public override Register? ReturnRegister(int byteCount)
        {
            return byteCount switch
            {
                0 => null,
                1 => ByteRegister.A,
                2 => WordRegister.BA,
                _ => throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, null)
            };
        }

        protected override LoadInstruction CreateByteLoadInstruction(Function function, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return new ByteLoadInstruction(function, destinationOperand, sourceOperand);
        }

        protected override LoadInstruction CreateWordLoadInstruction(Function function, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return new WordLoadInstruction(function, destinationOperand, sourceOperand);
        }

        public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand)
        {
            if (destinationOperand.Type.ByteCount == 1) {
                switch (operatorId) {
                    case '|':
                    case '^':
                    case '&':
                        return new ByteBitInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                    case '+':
                    case '-':
                        return new ByteAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                    case Keyword.ShiftLeft:
                    case Keyword.ShiftRight:
                        return new ByteShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                    default:
                        throw new NotImplementedException();
                }
            }
            switch (operatorId) {
                case '+':
                    return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case '-': {
                        if (rightOperand is IntegerOperand integerOperand && integerOperand.IntegerValue < 0) {
                            return new WordAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand, new IntegerOperand(rightOperand.Type, -integerOperand.IntegerValue));
                        }
                        return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                    }
                case '|':
                case '^':
                case '&':
                    return new WordBitInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case Keyword.ShiftLeft:
                case Keyword.ShiftRight:
                    return new WordShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                default:
                    throw new NotImplementedException();
            }
        }

        public override MonomialInstruction CreateMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            throw new NotImplementedException();
        }

        public override ResizeInstruction CreateResizeInstruction(Function function, AssignableOperand destinationOperand,
            IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
        {
            throw new NotImplementedException();
        }

        public override CompareInstruction CreateCompareInstruction(Function function, int operatorId, Operand leftOperand,
            Operand rightOperand, Anchor anchor)
        {
            throw new NotImplementedException();
        }

        public override JumpInstruction CreateJumpInstruction(Function function, Anchor anchor)
        {
            throw new NotImplementedException();
        }

        public override SubroutineInstruction CreateSubroutineInstruction(Function function, Function targetFunction,
            AssignableOperand? destinationOperand, List<Operand> sourceOperands)
        {
            throw new NotImplementedException();
        }

        public override ReturnInstruction CreateReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor)
        {
            throw new NotImplementedException();
        }

        public override DecrementJumpInstruction CreateDecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
        {
            throw new NotImplementedException();
        }

        public override MultiplyInstruction CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand,
            Operand leftOperand, int rightValue)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Register> IncludedRegisterIds(Register register)
        {
            throw new NotImplementedException();
        }

        public override Operand LowByteOperand(Operand operand)
        {
            throw new NotImplementedException();
        }

        public override Operand HighByteOperand(Operand operand)
        {
            throw new NotImplementedException();
        }

        public override void CallExternal(Instruction instruction, string functionName)
        {
            throw new NotImplementedException();
        }
        public override ReadOnlySpan<char> EndOfFunction => "\tret";

   }
}