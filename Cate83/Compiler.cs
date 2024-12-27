using System.Diagnostics;

namespace Inu.Cate.Sm83;

internal class Compiler() : Cate.Compiler(new ByteOperation(), new WordOperation())
{
    public const string TemporaryByte = "@Temporary@Byte";

    protected override void WriteAssembly(StreamWriter writer)
    {
        writer.Write("\text\t" + TemporaryByte);
        base.WriteAssembly(writer);
    }

    public override void AddSavingRegister(ISet<Register> registers, Register register)
    {
        if (register is ByteRegister { PairRegister: not null } byteRegister) {
            base.AddSavingRegister(registers, byteRegister.PairRegister);
        }
        else {
            base.AddSavingRegister(registers, register);
        }
    }

    public override void AllocateRegisters(List<Variable> variables, Function function)
    {
        var rangeOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null)
            .OrderBy(v => v.Range)
            .ThenBy(v => v.Usages.Count).ToList();
        AllocateOrdered(rangeOrdered);
        var usageOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null)
            .OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
        AllocateOrdered(usageOrdered);

        foreach (var variable in variables.Where(v => v.Register == null && !v.Static)) {
            if (variable.Parameter?.Register == null)
                continue;
            var register = variable.Parameter.Register;
            if (register is ByteRegister byteRegister && !Conflict(variable.Intersections, byteRegister)) {
                variable.Register = byteRegister;
            }
            else if (register is ByteRegister) {
                register = AllocatableRegister(variable, ByteRegister.Registers, function);
                if (register != null) {
                    variable.Register = register;
                }
            }
            else if (register is WordRegister) {
                register = AllocatableRegister(variable, WordRegister.Registers, function);
                if (register != null) {
                    variable.Register = register;
                }
            }
        }

        return;

        void AllocateOrdered(List<Variable> ordered)
        {
            foreach (var variable in ordered) {
                var variableType = variable.Type;
                Register? register;
                if (variableType.ByteCount == 1) {
                    var registers = ByteRegister.Registers;
                    register = AllocatableRegister(variable, registers, function);
                }
                else {
                    var registers = new List<WordRegister>() { WordRegister.Hl, WordRegister.De, WordRegister.Bc };
                    register = AllocatableRegister(variable, registers, function);
                }
                if (register == null)
                    continue;
                variable.Register = register;
            }
        }
    }

    public override Register? ParameterRegister(int index, ParameterizableType type)
    {
        return SubroutineInstruction.ParameterRegister(index, type);
    }

    public override Register? ReturnRegister(ParameterizableType type)
    {
        return SubroutineInstruction.ReturnRegister(type);
    }

    protected override LoadInstruction CreateWordLoadInstruction(Function function, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return new WordLoadInstruction(function, destinationOperand, sourceOperand);
    }

    public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand,
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
                return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand);
            case '-': {
                    if (rightOperand is IntegerOperand { IntegerValue: > 0 } integerOperand) {
                        var operand = new IntegerOperand(rightOperand.Type, -integerOperand.IntegerValue);
                        return new WordAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand, operand);
                    }
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

    public override MonomialInstruction CreateMonomialInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand,
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

    public override DecrementJumpInstruction CreateDecrementJumpInstruction(Function function,
        AssignableOperand operand, Anchor anchor)
    {
        return new DecrementJumpInstruction(function, operand, anchor);
    }

    public override ReadOnlySpan<char> EndOfFunction => "\tret";

    public override MultiplyInstruction CreateMultiplyInstruction(Function function,
        AssignableOperand destinationOperand,
        Operand leftOperand, int rightValue)
    {
        return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
    }

    public override IEnumerable<Register> IncludedRegisters(Register register)
    {
        if (register is WordRegister wordRegister) {
            return wordRegister.ByteRegisters;
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