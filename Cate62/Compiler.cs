using System.Diagnostics;

namespace Inu.Cate.Sc62015
{

    public class Compiler : Cate.Compiler
    {
        private const string TempByteLabel = "@TempByte";
        public const string TemporaryByte = "(<" + TempByteLabel + ")";
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

        public static int NewRegisterId()
        {
            return ++registerId;
        }

        public Compiler() : base(new ByteOperation(), new WordOperation(), new PointerOperation()) { }

        protected override void WriteAssembly(StreamWriter writer)
        {
            writer.WriteLine("\text " + TempByteLabel);
            writer.WriteLine("extrn " + ByteInternalRam.Prefix);
            writer.WriteLine("extrn " + WordInternalRam.Prefix);
            writer.WriteLine("extrn " + PointerInternalRam.Prefix);
            base.WriteAssembly(writer);
        }

        //public override ISet<Register> SavingRegisters(Register register)
        //{
        //    return new HashSet<Register>() { SavingRegister(register) };
        //}

        private static Register SavingRegister(Register register)
        {
            if (register is not ByteRegister byteRegister) {
                return register;
            }
            Debug.Assert(byteRegister.PairRegister != null);
            return byteRegister.PairRegister;
        }

        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            var ordered = variables.Where(v => v.Register == null && v is { Static: false })
                .OrderBy(v => v.Range)
                .ThenByDescending(v => v.Usages.Count).ToList();
            ;
            foreach (var variable in ordered) {
                var variableType = variable.Type;
                var register = variableType.ByteCount switch
                {
                    1 => AllocatableRegister(variable, ByteOperation.Registers),
                    2 => AllocatableRegister(variable, WordOperation.Registers),
                    3 => AllocatableRegister(variable, PointerOperation.Registers),
                    _ => null
                };
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
                    3 => PointerRegister.X,
                    _ => throw new ArgumentOutOfRangeException()
                },
                1 => type.ByteCount switch
                {
                    1 => ByteRegister.IL,
                    2 => WordRegister.I,
                    3 => PointerRegister.Y,
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => null
            };
        }

        public override Register? ReturnRegister(ParameterizableType type)
        {
            return type.ByteCount switch
            {
                1 => ByteRegister.A,
                2 => WordRegister.BA,
                3 => PointerRegister.X,
                _ => null
            };
        }

        protected override Cate.LoadInstruction CreateByteLoadInstruction(Function function, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return new ByteLoadInstruction(function, destinationOperand, sourceOperand);
        }

        protected override Cate.LoadInstruction CreateWordLoadInstruction(Function function, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return new WordLoadInstruction(function, destinationOperand, sourceOperand);
        }

        protected override Cate.LoadInstruction CreatePointerLoadInstruction(Function function, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return new PointerLoadInstruction(function, destinationOperand, sourceOperand);
        }

        public override Cate.BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand)
        {
            switch (destinationOperand.Type.ByteCount) {
                case 1:
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
                    }

                    break;
                case 2:
                    switch (operatorId) {
                        case '+':
                            return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                                rightOperand);
                        case '-': {
                                if (rightOperand is IntegerOperand { IntegerValue: < 0 } integerOperand) {
                                    return new WordAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand,
                                        new IntegerOperand(rightOperand.Type, -integerOperand.IntegerValue));
                                }

                                return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                                    rightOperand);
                            }
                        case '|':
                        case '^':
                        case '&':
                            return new WordBitInstruction(function, operatorId, destinationOperand, leftOperand,
                                rightOperand);
                        case Keyword.ShiftLeft:
                        case Keyword.ShiftRight:
                            return new WordShiftInstruction(function, operatorId, destinationOperand, leftOperand,
                                rightOperand);
                    }

                    break;
                case 3:
                    switch (operatorId) {
                        case '+':
                            return new PointerAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                                rightOperand);
                        case '-': {
                                if (rightOperand is IntegerOperand { IntegerValue: < 0 } integerOperand) {
                                    return new PointerAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand,
                                        new IntegerOperand(rightOperand.Type, -integerOperand.IntegerValue));
                                }

                                return new PointerAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                                    rightOperand);
                            }
                    }
                    break;
            }

            throw new NotImplementedException();
        }

        public override Cate.MonomialInstruction CreateMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return destinationOperand.Type.ByteCount switch
            {
                1 => new ByteMonomialInstruction(function, operatorId, destinationOperand, sourceOperand),
                2 => new WordMonomialInstruction(function, operatorId, destinationOperand, sourceOperand),
                _ => throw new NotImplementedException()
            };
        }

        public override Cate.ResizeInstruction CreateResizeInstruction(Function function, AssignableOperand destinationOperand,
            IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
        {
            return new ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType);
        }

        public override Cate.CompareInstruction CreateCompareInstruction(Function function, int operatorId, Operand leftOperand,
            Operand rightOperand, Anchor anchor)
        {
            return new CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor);
        }

        public override Cate.JumpInstruction CreateJumpInstruction(Function function, Anchor anchor)
        {
            return new JumpInstruction(function, anchor);
        }

        public override Cate.SubroutineInstruction CreateSubroutineInstruction(Function function, Function targetFunction,
            AssignableOperand? destinationOperand, List<Operand> sourceOperands)
        {
            return new SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands);
        }

        public override Cate.ReturnInstruction CreateReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor)
        {
            return new ReturnInstruction(function, sourceOperand, anchor);
        }

        public override Cate.DecrementJumpInstruction CreateDecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
        {
            return new DecrementJumpInstruction(function, operand, anchor);
        }

        public override Cate.MultiplyInstruction CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand,
            Operand leftOperand, int rightValue)
        {
            return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
        }

        public override IEnumerable<Register> IncludedRegisters(Register register)
        {
            if (Equals(register, WordRegister.BA))
                return new List<Register>() { ByteRegister.A };
            if (Equals(register, WordRegister.I))
                return new List<Register>() { ByteRegister.IL };
            return new List<Register>();
        }

        public override Operand LowByteOperand(Operand operand)
        {
            var newType = ToByteType(operand);
            return operand switch
            {
                ConstantOperand constantOperand => new StringOperand(newType,
                    "low(" + constantOperand.MemoryAddress() + ")"),
                VariableOperand variableOperand => variableOperand.Register switch
                {
                    WordRegister { Low: { } } wordRegister =>
                        //Debug.Assert(wordRegister.Low != null);
                        new ByteRegisterOperand(newType, wordRegister.Low),
                    _ => new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset)
                },
                IndirectOperand indirectOperand => new IndirectOperand(indirectOperand.Variable, newType,
                    indirectOperand.Offset),
                _ => throw new NotImplementedException()
            };
        }

        public override Operand HighByteOperand(Operand operand)
        {
            var newType = ToByteType(operand);
            switch (operand) {
                case ConstantOperand constantOperand:
                    return new StringOperand(newType, "high(" + constantOperand.MemoryAddress() + ")");
                case VariableOperand variableOperand:
                    Debug.Assert(variableOperand.Register == null);
                    return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                case IndirectOperand indirectOperand:
                    return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset + 1);
                default:
                    throw new NotImplementedException();
            }
        }

        public override void CallExternal(Instruction instruction, string functionName)
        {
            instruction.WriteLine("\tcall " + functionName);
            Instance.AddExternalName(functionName);
        }

        public override string PointerConstantDirective => "defp";
        public override int PointerByteCount => 3;
        public override void RemoveSavingRegister(ISet<Register> savedRegisters, int byteCount)
        {
            if (byteCount == 1) {
                savedRegisters.Remove(WordRegister.BA);
            }
            base.RemoveSavingRegister(savedRegisters, byteCount);
        }

        public override ReadOnlySpan<char> EndOfFunction => "\tret";


    }
}