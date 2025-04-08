using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Channels;

namespace Inu.Cate.Tlcs900;

internal class Compiler() : Inu.Cate.Compiler(new ByteOperation(), new WordOperation(), true)
{
    public override void SaveRegisters(StreamWriter writer, ISet<Register> registers, Function instruction)
    {
        foreach (var r in SavingRegisters(registers).ToImmutableSortedSet()) {
            r.Save(writer, null, null, 0);
        }
    }

    public override void SaveRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction, int tabCount)
    {
        var dictionary = DistinctRegisters(variables);
        foreach (var (register, list) in dictionary.OrderBy(p => p.Key)) {
            var comment = "\t; " + string.Join(',', list.Select(v => v.Name).ToArray());
            register.Save(writer, comment, instruction, tabCount);
        }
    }

    public override void RestoreRegisters(StreamWriter writer, ISet<Register> registers, int byteCount)
    {
        foreach (var register in SavingRegisters(registers).ToImmutableSortedSet().Reverse()) {
            RestoreRegister(writer, register, byteCount);
        }
    }

    public override void RestoreRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction, int tabCount)
    {
        var dictionary = DistinctRegisters(variables);
        foreach (var (register, list) in dictionary.OrderByDescending(p => p.Key)) {
            var comment = "\t; " + string.Join(',', list.Select(v => v.Name).ToArray());
            register.Restore(writer, comment, instruction, tabCount);
        }
    }

    public override void AllocateRegisters(List<Variable> variables, Function function)
    {
        var rangeOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderBy(v => v.Range)
            .ThenBy(v => v.Usages.Count).ToList();

        Allocate(rangeOrdered);
        var usageOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
        Allocate(usageOrdered);

        foreach (var variable in variables.Where(v => v.Register == null && !v.Static)) {
            if (variable.Parameter?.Register == null)
                continue;
            var register = variable.Parameter.Register;
            if (register is ByteRegister byteRegister) {
                if (!Conflict(variable.Intersections, byteRegister)) {
                    variable.Register = byteRegister;
                }
                else {
                    register = AllocatableRegister(variable, ByteRegister.All, function);
                    if (register != null) {
                        variable.Register = register;
                    }
                }
            }
            else if (register is WordRegister wordRegister) {
                var registers = WordRegister.All;
                if (registers.Contains(wordRegister) && !Conflict(variable.Intersections, wordRegister)) {
                    variable.Register = wordRegister;
                }
                else {
                    register = AllocatableRegister(variable, registers, function);
                    if (register != null) {
                        variable.Register = register;
                    }
                }
            }
        }

        return;

        void Allocate(List<Variable> list)
        {
            foreach (var variable in list) {
                var variableType = variable.Type;
                var register = variableType.ByteCount switch
                {
                    1 => AllocatableRegister(variable, ByteRegister.All, function),
                    _ => AllocatableRegister(variable, WordRegister.All, function)
                };
                if (register == null)
                    continue;
                variable.Register = register;
            }
        }
    }

    public override Register? ParameterRegister(int index, ParameterizableType type)
    {
        List<Register> registers = type.ByteCount switch
        {
            1 =>
            [
                ByteRegister.A, ByteRegister.C, ByteRegister.E, ByteRegister.L
            ],
            2 =>
            [
                WordRegister.WA, WordRegister.BC, WordRegister.DE, WordRegister.HL, WordRegister.IX,
                WordRegister.IY, WordRegister.IZ
            ],
            _ => throw new NotImplementedException()
        };
        return index < registers.Count ? registers[index] : null;
    }

    public override Register? ReturnRegister(ParameterizableType type)
    {
        return type.ByteCount switch
        {
            0 => null,
            1 => ByteRegister.A,
            2 => WordRegister.WA,
            _ => throw new NotImplementedException()
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

    public override Cate.BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand leftOperand, Operand rightOperand)
    {
        switch (destinationOperand.Type.ByteCount) {
            case 1:
                switch (operatorId) {
                    case '+':
                    case '-':
                        return new AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                            rightOperand);
                    case '|':
                    case '^':
                    case '&':
                        return new BinomialInstruction(function, operatorId, destinationOperand, leftOperand,
                            rightOperand);
                    case Keyword.ShiftLeft:
                    case Keyword.ShiftRight:
                        return new ShiftInstruction(function, operatorId, destinationOperand, leftOperand,
                            rightOperand);
                }
                break;
            case 2:
                switch (operatorId) {
                    case '+':
                    case '-':
                        return new AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                            rightOperand);
                    case '|':
                    case '^':
                    case '&':
                        return new BinomialInstruction(function, operatorId, destinationOperand, leftOperand,
                            rightOperand);
                    case Keyword.ShiftLeft:
                    case Keyword.ShiftRight:
                        return new ShiftInstruction(function, operatorId, destinationOperand, leftOperand,
                            rightOperand);
                }
                break;
        }
        throw new NotImplementedException();
    }

    public override MonomialInstruction CreateMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return new MonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
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
        return new List<Register>();
    }

    public override Operand LowByteOperand(Operand operand)
    {
        var newType = ToByteType(operand);
        switch (operand) {
            case ConstantOperand constantOperand:
                return new StringOperand(newType, "low(" + constantOperand.MemoryAddress() + ")");
            case VariableOperand variableOperand:
                Debug.Assert(variableOperand.Register == null);
                return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset);
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

    public override void RemoveSavingRegister(ISet<Register> savedRegisters, Register returnRegister)
    {
        if (returnRegister is ByteRegister byteRegister) {
            bool changed;
            do {
                changed = false;
                foreach (var savedRegister in savedRegisters) {
                    if (savedRegister is not WordRegister wordRegister) continue;
                    if (byteRegister.Equals(wordRegister.Low)) {
                        savedRegisters.Remove(wordRegister);
                        Debug.Assert(wordRegister.High != null);
                        savedRegisters.Add(wordRegister.High);
                        changed = true;
                        break;
                    }
                    if (byteRegister.Equals(wordRegister.High)) {
                        savedRegisters.Remove(wordRegister);
                        Debug.Assert(wordRegister.Low != null);
                        savedRegisters.Add(wordRegister.Low);
                        changed = true;
                        break;
                    }
                }
            } while (changed);
        }
        base.RemoveSavingRegister(savedRegisters, returnRegister);
    }


    public void OperateMemory(Instruction instruction, Operand operand, Action<string> action, bool change)
    {
        switch (operand) {
            case VariableOperand variableOperand:
                Debug.Assert(variableOperand.Register == null);
                action("(" + variableOperand.MemoryAddress() + ")");
                if (change) {
                    instruction.RemoveVariableRegister(variableOperand);
                }
                return;
            case IndirectOperand indirectOperand: {
                    var pointer = indirectOperand.Variable;
                    var pointerRegister = instruction.GetVariableRegister(pointer, 0);
                    if (pointerRegister is WordRegister wordRegister) {
                        ViaRegister(wordRegister);
                        return;
                    }
                    using var reservation = WordOperation.ReserveAnyRegister(instruction);
                    reservation.WordRegister.LoadFromMemory(instruction, pointer, 0);
                    ViaRegister(reservation.WordRegister);
                    return;
                }
                void ViaRegister(Cate.WordRegister pointerRegister)
                {
                    var offset = indirectOperand.Offset;
                    if (offset == 0) {
                        action("(" + pointerRegister + ")");
                    }
                    else {
                        action("(" + pointerRegister + "+" + offset + ")");
                    }
                }
        }
        throw new NotImplementedException();
    }
}