﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Tms99;

internal class Compiler() : Cate.Compiler(new ByteOperation(), new WordOperation())
{
    public override void AddSavingRegister(ISet<Register> registers, Register register)
    {
        switch (register) {
            case ByteRegister byteRegister:
                base.AddSavingRegister(registers, byteRegister.WordRegister);
                break;
            case WordRegister wordRegister:
                base.AddSavingRegister(registers, wordRegister);
                break;
            default:
                base.AddSavingRegister(registers, register);
                break;
        }
    }

    public override void AllocateRegisters(List<Variable> variables, Function function)
    {
        var rangeOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null)
            .OrderBy(v => v.Range)
            .ThenByDescending(v => v.Usages.Count).ToList();
        foreach (var variable in rangeOrdered) {
            var variableType = variable.Type;
            var register = variableType.ByteCount == 1 ? AllocatableRegister(variable, ByteRegister.Registers, function) : AllocatableRegister(variable, WordRegister.Registers, function);
            if (register != null) {
                variable.Register = register;
            }
        }

        var usageOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
        foreach (var variable in usageOrdered) {
            var variableType = variable.Type;
            var register = variableType.ByteCount == 1 ? AllocatableRegister(variable, ByteRegister.Registers, function) : AllocatableRegister(variable, WordRegister.Registers, function);
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
            }
        }
    }

    public override Register? ParameterRegister(int index, ParameterizableType type)
    {
        if (index >= 10) return null;
        return type.ByteCount switch
        {
            1 => ByteRegister.FromIndex(index),
            2 => WordRegister.FromIndex(index),
            _ => throw new NotImplementedException()
        };
    }

    public override Register? ReturnRegister(ParameterizableType type)
    {
        Register? register = type.ByteCount switch
        {
            1 => ByteRegister.FromIndex(0),
            2 => WordRegister.FromIndex(0),
            _ => null
        };
        return register;
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
            return operatorId switch
            {
                '|' => new ByteBitInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand),
                '^' => new ByteBitInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand),
                '&' => new ByteBitInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand),
                '+' => new ByteAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                '-' => new ByteAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                Keyword.ShiftLeft => new ByteShiftInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                Keyword.ShiftRight => new ByteShiftInstruction(function, operatorId, destinationOperand,
                    leftOperand, rightOperand),
                _ => throw new NotImplementedException()
            };
        }
        switch (operatorId) {
            case '+':
                return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
            case '-': {
                    if (rightOperand is IntegerOperand { IntegerValue: > 0 } integerOperand) {
                        var operand = new IntegerOperand(rightOperand.Type, -integerOperand.IntegerValue);
                        return new WordAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand, operand);
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
        if (destinationOperand.Type.ByteCount == 1) {
            return new ByteMonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
        }
        return new WordMonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
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

    public override void WriteBeginningOfFunction(StreamWriter writer, Function function)
    {
        if (function.Instructions.Any(i => i.IsCalling())) {
            WordRegister.Save(writer, 11, null);
        }
    }

    public override ReadOnlySpan<char> EndOfFunction => "rt";
    public override void WriteEndOfFunction(StreamWriter writer, Function function)
    {
        if (function.Instructions.Any(i => i.IsCalling())) {
            WordRegister.Restore(writer, 11, null);
        }
        base.WriteEndOfFunction(writer, function);
    }

    public override int Alignment => 2;
    public override IntegerType CounterType => IntegerType.WordType;
    public override string ParameterPrefix => "__";

    public override IEnumerable<Register> IncludedRegisters(Register register)
    {
        return register switch
        {
            ByteRegister byteRegister => [byteRegister.WordRegister],
            WordRegister wordRegister => [wordRegister.ByteRegister],
            _ => throw new NotImplementedException()
        };
    }

    public override Operand LowByteOperand(Operand operand)
    {
        var newType = ToByteType(operand);
        switch (operand) {
            case ConstantOperand constantOperand:
                return new StringOperand(newType, "low(" + constantOperand.MemoryAddress() + ")");
            case VariableOperand variableOperand:
                if (variableOperand.Register != null) {
                    if (variableOperand.Register is WordRegister wordRegister) {
                        return new ByteRegisterOperand(ToByteType(operand), wordRegister.ByteRegister);
                    }
                }
                else {
                    return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                }

                break;
            case IndirectOperand indirectOperand:
                return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset + 1);
        }
        throw new NotImplementedException();
    }

    public override Operand HighByteOperand(Operand operand)
    {
        var newType = ToByteType(operand);
        switch (operand) {
            case ConstantOperand constantOperand:
                return new StringOperand(newType, "high(" + constantOperand.MemoryAddress() + ")");
            case VariableOperand variableOperand:
                if (variableOperand.Register != null) {
                    if (variableOperand.Register is Cate.WordRegister { Low: { } } wordRegister) {
                        Debug.Assert(wordRegister.Low != null);
                        return new ByteRegisterOperand(ToByteType(operand), wordRegister.Low);
                    }
                }
                else {
                    return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset);
                }
                break;
            case IndirectOperand indirectOperand:
                return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset);
        }
        throw new NotImplementedException();
    }

    public override void CallExternal(Instruction instruction, string functionName)
    {
        instruction.WriteLine("\tbl\t@" + functionName);
        Instance.AddExternalName(functionName);
    }

    public static bool Operate(Instruction instruction, string operation, Operand sourceOperand, AssignableOperand destinationOperand)
    {
        var source = OperandToString(instruction, sourceOperand, false);
        var destination = OperandToString(instruction, destinationOperand, true);
        if (source == null || destination == null) return false;
        instruction.WriteLine("\t" + operation + "\t" + source + "," + destination);
        return true;
    }

    public static string? OperandToString(Instruction instruction, Operand operand, bool change)
    {
        switch (operand) {
            case VariableOperand variableOperand: {
                    var variable = variableOperand.Variable;
                    var offset = variableOperand.Offset;
                    var register = variable.Register;
                    if (register == null && !change) {
                        register = instruction.GetVariableRegister(variable, offset);
                    }
                    if (register != null) {
                        return register.Name;
                    }
                    return "@" + variable.MemoryAddress(offset);
                }
            case IndirectOperand indirectOperand: {
                    var pointer = indirectOperand.Variable;
                    var offset = indirectOperand.Offset;
                    if (pointer.Register is WordRegister pointerRegister && pointerRegister.IsOffsetInRange(offset)) {
                        if (offset == 0) {
                            return "*" + pointerRegister.Name;
                        }
                        return "@" + offset + "(" + pointerRegister.Name + ")";
                    }
                    break;
                }
        }
        return null;
    }

    public override string LabelPrefix => "__";

    public static bool Operate(Instruction instruction, string operation, AssignableOperand destinationOperand)
    {
        var destination = OperandToString(instruction, destinationOperand, true);
        if (destination == null) return false;
        instruction.WriteLine("\t" + operation + "\t" + destination);
        return true;
    }
}