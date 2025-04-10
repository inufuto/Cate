﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static System.String;

namespace Inu.Cate.Mc6809;

internal class Compiler : Cate.Compiler
{
    public Compiler() : base(new ByteOperation(), new WordOperation())
    { }

    protected override void WriteAssembly(StreamWriter writer)
    {
        writer.WriteLine("\text\t" + DirectPage.Byte.Label);
        writer.WriteLine("\text\t" + DirectPage.Word.Label);
        writer.WriteLine("\text\t" + DirectPage.Word2.Label);
        base.WriteAssembly(writer);
    }

    public override void AddSavingRegister(ISet<Register> registers, Register register)
    {
        if (Equals(register, WordRegister.D)) {
            registers.Add(ByteRegister.A);
            registers.Add(ByteRegister.B);
            return;
        }
        if (Equals(register, ByteRegister.A) || Equals(register, ByteRegister.B)) {
            registers.Add(register);
            return;
        }
        base.AddSavingRegister(registers, register);
    }


    private IEnumerable<Register> SavingRegisters(IEnumerable<Variable> variables)
    {
        var savingRegisterIds = new HashSet<Register>();
        foreach (var variable in variables) {
            Debug.Assert(variable.Register != null);
            var registers = SavingRegisters(variable.Register);
            foreach (var register in registers) {
                savingRegisterIds.Add(register);
            }
        }
        return savingRegisterIds;
    }

    public override void SaveRegisters(StreamWriter writer, ISet<Register> registers, Function instruction)
    {
        SaveRegisters(writer, registers, null, 0);
    }

    public override void SaveRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction, int tabCount)
    {
        var list = variables.ToList();
        var savingRegisterIds = SavingRegisters(list);
        var comment = "\t; " + Join(',', list.Select(v => v.Name).ToArray());
        SaveRegisters(writer, savingRegisterIds, comment, tabCount);
    }

    private static void SaveRegisters(StreamWriter writer, IEnumerable<Register> registers, string? comment, int tabCount)
    {
        var list = registers.ToList();
        if (!list.Any())
            return;

        Debug.Assert(!list.Contains(WordRegister.D));

        list.Sort();
        var names = Join(',', list.Select(r => r.Name));
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpshs\t" + names + comment);
    }
    public override void RestoreRegisters(StreamWriter writer, ISet<Register> registers, int byteCount)
    {
        RestoreRegisters(writer, registers, null, 0);
    }

    public override void RestoreRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction, int tabCount)
    {
        var savingRegisterIds = SavingRegisters(variables);
        var comment = "\t; " + Join(',', variables.Select(v => v.Name).ToArray());
        RestoreRegisters(writer, savingRegisterIds, comment, tabCount);
    }

    private static void RestoreRegisters(StreamWriter writer, IEnumerable<Register> registers, string? comment, int tabCount)
    {
        var list = registers.ToList();
        if (!list.Any())
            return;

        list.Sort();
        var names = Join(',', list.Select(r => r.Name));
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpuls\t" + names + comment);
    }


    public override void AllocateRegisters(List<Variable> variables, Function function)
    {
        var rangeOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null)
            .OrderBy(v => v.Range)
            .ThenByDescending(v => v.Usages.Count).ToList();
        ;
        foreach (var variable in rangeOrdered) {
            var variableType = variable.Type;
            Cate.Register? register = null;
            if (variableType.ByteCount != 1) continue;
            register = AllocatableRegister(variable, ByteRegister.Registers, function);
            if (register != null) {
                variable.Register = register;
            }
        }

        var usageOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
        foreach (var variable in usageOrdered) {
            var variableType = variable.Type;
            Cate.Register? register;
            if (variableType.ByteCount == 1) {
                register = AllocatableRegister(variable, ByteRegister.Registers, function);
            }
            else {
                List<Cate.WordRegister> registers = variableType is PointerType ? WordRegister.IndexRegisters : WordRegister.Registers;
                register = AllocatableRegister(variable, registers, function);
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


    public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand,
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

    public override MonomialInstruction CreateMonomialInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand, Operand sourceOperand)
    {
        if (destinationOperand.Type.ByteCount == 1) {
            return new ByteMonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
        }
        return new WordMonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
    }

    public override Cate.ResizeInstruction CreateResizeInstruction(Function function,
        AssignableOperand destinationOperand,
        IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
    {
        return new ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType);
    }

    public override Cate.CompareInstruction CreateCompareInstruction(Function function, int operatorId,
        Operand leftOperand, Operand rightOperand,
        Anchor anchor)
    {
        return new CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor);
    }

    public override Cate.JumpInstruction CreateJumpInstruction(Function function, Anchor anchor)
    {
        return new JumpInstruction(function, anchor);
    }

    public override Cate.SubroutineInstruction CreateSubroutineInstruction(Function function,
        Function targetFunction,
        AssignableOperand? destinationOperand,
        List<Operand> sourceOperands)
    {
        return new SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands);
    }

    public override Cate.ReturnInstruction CreateReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor)
    {
        return new ReturnInstruction(function, sourceOperand, anchor);
    }

    public override Cate.DecrementJumpInstruction CreateDecrementJumpInstruction(Function function,
        AssignableOperand operand, Anchor anchor)
    {
        return new DecrementJumpInstruction(function, operand, anchor);
    }

    public override ReadOnlySpan<char> EndOfFunction => "\trts";

    public override Cate.MultiplyInstruction CreateMultiplyInstruction(Function function,
        AssignableOperand destinationOperand, Operand leftOperand,
        int rightValue)
    {
        return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
    }

    public override IEnumerable<Register> IncludedRegisters(Register? register)
    {
        return Equals(register, WordRegister.D) ? new List<Register>() { ByteRegister.A, ByteRegister.B } : new List<Register>();
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

    public override Operand LowByteOperand(Operand operand)
    {
        var newType = ToByteType(operand);
        switch (operand) {
            case ConstantOperand constantOperand:
                return new StringOperand(newType, "low(" + constantOperand.MemoryAddress() + ")");
            case VariableOperand variableOperand:
                if (variableOperand.Register != null) {
                    if (variableOperand.Register is Cate.WordRegister { High: { } } wordRegister) {
                        Debug.Assert(wordRegister.High != null);
                        return new ByteRegisterOperand(ToByteType(operand), wordRegister.High);
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

    public override void CallExternal(Instruction instruction, string externalName)
    {
        instruction.WriteLine("\tjsr\t" + externalName);
        Instance.AddExternalName(externalName);
    }
}