using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class Compiler() : Cate.Compiler(new ByteOperation(), new WordOperation(), new PointerOperation())
{
    public const string ZeroPageLabel = "__rf";

    protected override void WriteAssembly(StreamWriter writer)
    {
        writer.WriteLine("ext " + ZeroPageLabel);
        base.WriteAssembly(writer);
    }

    public override void AddSavingRegister(ISet<Register> registers, Register register)
    {
        switch (register) {
            case WordRegister wordRegister:
                Debug.Assert(wordRegister is { High: not null, Low: not null });
                registers.Add(wordRegister.High);
                registers.Add(wordRegister.Low);
                break;
            case PointerRegister pointerRegister:
                AddSavingRegister(registers, pointerRegister.WordRegister);
                break;
            default:
                registers.Add(register);
                break;
        }
    }

    private class RegisterComment(Register register, string comment = "")
    {
        public readonly Register Register = register;
        public readonly string Comment = comment;

        public static List<RegisterComment> FromVariables(IEnumerable<Variable> variables)
        {
            var list = new List<RegisterComment>();
            foreach (var variable in variables) {
                var register = variable.Register;
                if (register == null) continue;
                list.Add(new RegisterComment(register, variable.Name));
            }
            return Aggregate(list);
        }

        public static List<RegisterComment> FromRegisters(ISet<Register> registers)
        {
            return Aggregate(registers.Select(register => new RegisterComment(register)).ToList());
        }

        private static List<RegisterComment> Aggregate(List<RegisterComment> source)
        {
            source.Sort(Compare);
            var destination = new List<RegisterComment>();
            for (var i = 0; i < source.Count; ++i) {
                var current = source[i];
                if (i < source.Count - 1 && current.Register is ByteRegister currentRegister && (currentRegister.Address & 1) == 0) {
                    var next = source[i + 1];
                    if (next.Register is ByteRegister nextRegister && nextRegister.Address == currentRegister.Address + 1) {
                        Debug.Assert(currentRegister.PairRegister != null);
                        destination.Add(new RegisterComment(currentRegister.PairRegister, current.Comment + " " + next.Comment));
                        ++i;
                        continue;
                    }
                }
                destination.Add(current);
            }
            return destination;
        }

        private static int Compare(RegisterComment a, RegisterComment b)
        {
            return Order(a.Register) - Order(b.Register);
        }

        private static int Order(Register register)
        {
            return register switch
            {
                ByteRegister byteRegister => byteRegister.Address,
                WordRegister wordRegister => wordRegister.Address,
                PointerRegister pointerRegister => Order(pointerRegister.WordRegister),
                _ => 0x100 + register.Id
            };
        }
    }

    public override void SaveRegisters(StreamWriter writer, ISet<Register> registers)
    {
        var registerComments = RegisterComment.FromRegisters(registers);
        foreach (var registerComment in registerComments) {
            var comment = !string.IsNullOrEmpty(registerComment.Comment) ? " ;" + registerComment.Comment : "";
            registerComment.Register.Save(writer, comment, null, 0);
        }
    }

    public override void SaveRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction, int tabCount)
    {
        var registerComments = RegisterComment.FromVariables(variables);
        foreach (var registerComment in registerComments) {
            var comment = !string.IsNullOrEmpty(registerComment.Comment) ? " ;" + registerComment.Comment : "";
            registerComment.Register.Save(writer, comment, instruction, tabCount);
        }
    }

    public override void RestoreRegisters(StreamWriter writer, ISet<Register> registers, int byteCount)
    {
        var registerComments = RegisterComment.FromRegisters(registers);
        registerComments.Reverse();
        foreach (var registerComment in registerComments) {
            var comment = !string.IsNullOrEmpty(registerComment.Comment) ? " ;" + registerComment.Comment : "";
            registerComment.Register.Restore(writer, comment, null, 0);
        }
    }

    public override void RestoreRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction, int tabCount)
    {
        var registerComments = RegisterComment.FromVariables(variables);
        registerComments.Reverse();
        foreach (var registerComment in registerComments) {
            var comment = !string.IsNullOrEmpty(registerComment.Comment) ? " ;" + registerComment.Comment : "";
            registerComment.Register.Restore(writer, comment, instruction, tabCount);
        }
    }

    public override void AllocateRegisters(List<Variable> variables, Function function)
    {
        var rangeOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null })
                .OrderBy(v => v.Range)
                .ThenByDescending(v => v.Usages.Count).ToList();
        foreach (var variable in rangeOrdered) {
            var variableType = variable.Type;
            Register? register;
            if (variableType.ByteCount == 1) {
                register = AllocatableRegister(variable, ByteOperation.Registers, function);
            }
            else if (variableType is PointerType) {
                register = AllocatableRegister(variable, PointerOperation.Registers, function);
            }
            else {
                register = AllocatableRegister(variable, WordOperation.Registers, function);
            }
            if (register != null) {
                variable.Register = register;
            }
        }

        var usageOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
        foreach (var variable in usageOrdered) {
            var variableType = variable.Type;
            Register? register;
            if (variableType.ByteCount == 1) {
                register = AllocatableRegister(variable, ByteOperation.Registers, function);
            }
            else if (variableType is PointerType) {
                register = AllocatableRegister(variable, PointerOperation.Registers, function);
            }
            else {
                register = AllocatableRegister(variable, WordOperation.Registers, function);
            }
            if (register == null)
                continue;
            variable.Register = register;
        }

        foreach (var variable in variables.Where(v => v.Register == null && !v.Static)) {
            if (variable.Parameter?.Register == null)
                continue;
            var register = variable.Parameter.Register;
            switch (register) {
                case ByteRegister byteRegister when !Conflict(variable.Intersections, byteRegister):
                    variable.Register = byteRegister;
                    break;
                case ByteRegister _: {
                        register = AllocatableRegister(variable, ByteOperation.Registers, function);
                        if (register != null) {
                            variable.Register = register;
                        }
                        break;
                    }
                case WordRegister wordRegister when !Conflict(variable.Intersections, wordRegister):
                    variable.Register = wordRegister;
                    break;
                case WordRegister _: {
                        register = AllocatableRegister(variable, WordOperation.Registers, function);
                        if (register != null) {
                            variable.Register = register;
                        }
                        break;
                    }
                //case PointerRegister pointerRegister when !Conflict(variable.Intersections, pointerRegister):
                //    variable.Register = pointerRegister;
                //    break;
                case PointerRegister _: {
                        register = AllocatableRegister(variable, PointerOperation.Registers, function);
                        if (register != null) {
                            variable.Register = register;
                        }
                        break;
                    }
            }
        }
    }

    public override Register? ParameterRegister(int index, ParameterizableType type)
    {
        if (index >= 4) return null;
        var address = index * 2;
        return type.ByteCount switch
        {
            1 => ByteRegister.FromAddress(address + 1),
            _ => type is PointerType ? PointerRegister.FromAddress(address) : WordRegister.FromAddress(address)
        };
    }

    public override Register? ReturnRegister(ParameterizableType type)
    {
        return type.ByteCount switch
        {
            1 => ByteRegister.FromAddress(1),
            2 => (type is PointerType ? PointerRegister.FromAddress(0) : WordRegister.FromAddress(0)),
            _ => null
        };
    }

    public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand leftOperand, Operand rightOperand)
    {
        if (destinationOperand.Type.ByteCount == 1) {
            return operatorId switch
            {
                '|' or '^' or '&' => new ByteBitInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                '+' or '-' => new ByteAddOrSubtractInstruction(function, operatorId, destinationOperand,
                    leftOperand, rightOperand),
                Keyword.ShiftLeft or Keyword.ShiftRight => new ByteShiftInstruction(function, operatorId,
                    destinationOperand, leftOperand, rightOperand),
                _ => throw new NotImplementedException()
            };
        }

        switch (operatorId) {
            case '+':
                if (destinationOperand.Type is PointerType)
                    return new PointerAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                        rightOperand);
                return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand);
            case '-': {
                    if (rightOperand is IntegerOperand { IntegerValue: > 0 } integerOperand) {
                        var operand = new IntegerOperand(rightOperand.Type, -integerOperand.IntegerValue);
                        if (destinationOperand.Type is PointerType)
                            return new PointerAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand,
                                operand);
                        return new WordAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand, operand);
                    }

                    if (destinationOperand.Type is PointerType)
                        return new PointerAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                            rightOperand);
                    return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                        rightOperand);
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
        return destinationOperand.Type.ByteCount switch
        {
            // + - ~
            1 => new ByteMonomialInstruction(function, operatorId, destinationOperand, sourceOperand),
            _ => new WordMonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
        };
    }

    public override ResizeInstruction CreateResizeInstruction(Function function, AssignableOperand destinationOperand,
        IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
    {
        return new ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType);
    }

    public override CompareInstruction CreateCompareInstruction(Function function, int operatorId, Operand leftOperand,
        Operand rightOperand, Anchor anchor)
    {
        return new CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor);
    }

    public override JumpInstruction CreateJumpInstruction(Function function, Anchor anchor)
    {
        return new JumpInstruction(function, anchor);
    }

    public override SubroutineInstruction CreateSubroutineInstruction(Function function, Function targetFunction,
        AssignableOperand? destinationOperand, List<Operand> sourceOperands)
    {
        return new SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands);
    }

    public override ReturnInstruction CreateReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor)
    {
        return new ReturnInstruction(function, sourceOperand, anchor);
    }

    public override DecrementJumpInstruction CreateDecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
    {
        return new DecrementJumpInstruction(function, operand, anchor);
    }

    public override ReadOnlySpan<char> EndOfFunction => "\tret";

    public override MultiplyInstruction CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand,
        Operand leftOperand, int rightValue)
    {
        return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
    }

    public override IEnumerable<Register> IncludedRegisters(Register register)
    {
        if (register is WordRegister wordRegister) {
            return wordRegister.ByteRegisters;
        }
        if (register is PointerRegister pointerRegister) {
            var registers = ((WordRegister)pointerRegister.WordRegister).ByteRegisters.ToList();
            return registers.Union([pointerRegister.WordRegister]);
        }
        return new List<Register>();
    }

    public override Operand LowByteOperand(Operand operand)
    {
        var newType = ToByteType(operand);
        switch (operand) {
            case ConstantOperand constantOperand:
                return new StringOperand(newType, "low(" + constantOperand.MemoryAddress() + ")");
            case VariableOperand variableOperand:
                switch (variableOperand.Register) {
                    case Cate.WordRegister wordRegister:
                        Debug.Assert(wordRegister.Low != null);
                        return new ByteRegisterOperand(newType, wordRegister.Low);
                    case Cate.PointerRegister pointerRegister:
                        Debug.Assert(pointerRegister.WordRegister != null);
                        Debug.Assert(pointerRegister.WordRegister.Low != null);
                        return new ByteRegisterOperand(newType, pointerRegister.WordRegister.Low);
                    default:
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset);
                }

            case IndirectOperand indirectOperand:
                return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset);
            default:
                throw new NotImplementedException();
        }
    }

    public override Operand HighByteOperand(Operand operand)
    {
        var newType = ToByteType(operand);
        switch (operand) {
            case ConstantOperand constantOperand:
                return new StringOperand(newType, "high(" + constantOperand.MemoryAddress() + ")");
            case VariableOperand variableOperand:
                switch (variableOperand.Register) {
                    case Cate.WordRegister wordRegister:
                        Debug.Assert(wordRegister.High != null);
                        return new ByteRegisterOperand(newType, wordRegister.High);
                    case Cate.PointerRegister pointerRegister:
                        Debug.Assert(pointerRegister.WordRegister != null);
                        Debug.Assert(pointerRegister.WordRegister.High != null);
                        return new ByteRegisterOperand(newType, pointerRegister.WordRegister.High);
                    default:
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                }
            case IndirectOperand indirectOperand:
                return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset + 1);
            default:
                throw new NotImplementedException();
        }
    }

    public override void CallExternal(Instruction instruction, string functionName)
    {
        instruction.WriteLine("\tcall\t" + functionName);
        Instance.AddExternalName(functionName);
    }
}