using System.Diagnostics;
using System.Xml.Linq;

namespace Inu.Cate.Hd61700;

internal class Compiler : Cate.Compiler
{
    internal const string RegisterHead = "$";

    public Compiler() : base(new ByteOperation(), new WordOperation(), new PointerOperation()) { }

    public override void SaveRegisters(StreamWriter writer, ISet<Register> registers)
    {
        var listList = ListList(registers);
        listList.Reverse();
        foreach (var list in listList) {
            if (list.Count > 1) {
                var register = list.Last();
                string name = register switch
                {
                    ByteRegister byteRegister => byteRegister.AsmName,
                    WordRegister wordRegister => wordRegister.HighByteName,
                    WordPointerRegister wordPointerRegister =>
                        ((WordRegister)wordPointerRegister.WordRegister).HighByteName,
                    _ => throw new NotImplementedException()
                };
                var count = list.Count * register.ByteCount;
                writer.WriteLine("\tphsm " + name + "," + count);
            }
            else {
                list.Last().Save(writer, "", false, 0);
            }
        }
    }

    public override void RestoreRegisters(StreamWriter writer, ISet<Register> registers, int byteCount)
    {
        var listList = ListList(registers);
        foreach (var list in listList) {
            if (list.Count > 1) {
                var register = list.First();
                Debug.Assert(register != null);
                var name = register.AsmName;
                var count = list.Count * register.ByteCount;
                writer.WriteLine("\tppsm " + name + "," + count);
            }
            else {
                list.First().Restore(writer, "", false, 0); 
            }
        }
    }


    private static List<List<Register>> ListList(ISet<Register> registers)
    {
        var list = registers.ToList();
        list.Sort();

        var listList = new List<List<Register>>();
        List<Register>? currentList = null;
        Register? prevRegister = null;
        foreach (var register in list) {
            if (currentList == null || prevRegister == null || prevRegister.Id + prevRegister.ByteCount != register.Id) {
                currentList = new List<Register>();
                listList.Add(currentList);
            }

            currentList.Add(register);
            prevRegister = register;
        }

        return listList;
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
                register = AllocatableRegister(variable, ByteRegister.Registers, function);
            }
            else if (variableType is PointerType) {
                register = AllocatableRegister(variable, WordPointerRegister.Registers, function);
            }
            else {
                register = AllocatableRegister(variable, WordRegister.Registers, function);
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
                register = AllocatableRegister(variable, ByteRegister.Registers, function);
            }
            else if (variableType is PointerType) {
                var registers = variableType is PointerType { ElementType: StructureType _ } ? PointerOperation.Registers : WordPointerRegister.Registers;
                register = AllocatableRegister(variable, registers, function);
            }
            else {
                register = AllocatableRegister(variable, WordRegister.Registers, function);
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
                        register = AllocatableRegister(variable, ByteRegister.Registers, function);
                        if (register != null) {
                            variable.Register = register;
                        }
                        break;
                    }
                case WordRegister wordRegister when !Conflict(variable.Intersections, wordRegister):
                    variable.Register = wordRegister;
                    break;
                case WordRegister _: {
                        register = AllocatableRegister(variable, WordRegister.Registers, function);
                        if (register != null) {
                            variable.Register = register;
                        }
                        break;
                    }
                case PointerRegister pointerRegister when !Conflict(variable.Intersections, pointerRegister):
                    variable.Register = pointerRegister;
                    break;
                case PointerRegister _: {
                        register = AllocatableRegister(variable, WordPointerRegister.Registers, function);
                        if (register != null) {
                            variable.Register = register;
                        }
                        break;
                    }
            }
        }
    }

    private static Cate.Register? AllocatableRegister<T>(Variable variable, IEnumerable<T> registers, Function function) where T : Cate.Register
    {
        foreach (var register in registers) {
            if (!IsAllocatable(function, variable, register)) continue;
            var conflict = Conflict(variable.Intersections, register);
            if (!conflict) return register;
        }
        return null;
    }

    private static bool IsAllocatable<T>(Function function, Variable variable, T register) where T : Register
    {
        return function.Instructions.All(i => i.CanAllocateRegister(variable, register));
    }

    private static bool Conflict<T>(IEnumerable<Variable> variables, T register) where T : Register
    {
        return variables.Any(v =>
            v.Register != null && register.Conflicts(v.Register));
    }

    public override Register? ParameterRegister(int index, ParameterizableType type)
    {
        return type.ByteCount switch
        {
            1 => ByteRegister.FromIndex(index),
            2 when type is PointerType => WordPointerRegister.FromIndex(index),
            2 => WordRegister.FromIndex(index),
            _ => throw new NotImplementedException()
        };
    }

    public override Register? ReturnRegister(ParameterizableType type)
    {
        return type.ByteCount switch
        {
            0 => null,
            1 => ByteRegister.FromIndex(0),
            2 when type is PointerType => WordPointerRegister.FromIndex(0),
            2 => WordRegister.FromIndex(0),
            _ => throw new NotImplementedException()
        };
    }

    public override Cate.BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand leftOperand, Operand rightOperand)
    {
        return destinationOperand.Type.ByteCount switch
        {
            1 => operatorId switch
            {
                Keyword.ShiftLeft => new ByteShiftInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                Keyword.ShiftRight => new ByteShiftInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                _ => new ByteBinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand),
            },
            2 when destinationOperand.Type is PointerType => new PointerAddOrSubtractInstruction(function, operatorId,
                destinationOperand, leftOperand, rightOperand),
            2 => operatorId switch
            {
                Keyword.ShiftLeft => new WordShiftInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                Keyword.ShiftRight => new WordShiftInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                _ => new WordBinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand),
            },
            _ => throw new NotImplementedException()
        };
    }

    public override MonomialInstruction CreateMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        switch (destinationOperand.Type.ByteCount) {
            case 1:
                return new ByteMonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
            case 2:
                if (destinationOperand.Type is not PointerType)
                    return new WordMonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
                break;
        }
        throw new NotImplementedException();
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

    public override MultiplyInstruction CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand,
        Operand leftOperand, int rightValue)
    {
        return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
    }
    public override ReadOnlySpan<char> EndOfFunction => "rtn";

    public override IEnumerable<Register> IncludedRegisters(Register register)
    {
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
        instruction.WriteLine("\tcal " + functionName);
        Instance.AddExternalName(functionName);
    }

    public static bool IsOffsetInRange(int offset)
    {
        return Math.Abs(offset) < 128;
    }
}