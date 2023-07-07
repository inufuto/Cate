﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class Compiler : Cate.Compiler
    {
        public Compiler() : base(new ByteOperation(), new WordOperation(), new PointerOperation()) { }

        protected override void WriteAssembly(StreamWriter writer)
        {
            writer.WriteLine("\textrn @Temporary@Byte");
            base.WriteAssembly(writer);
        }

        public override void AddSavingRegister(ISet<Register> registers, Register register)
        {
            if (register is ByteRegister { PairRegister: { } } byteRegister) {
                base.AddSavingRegister(registers, byteRegister.PairRegister);
            }
            else {
                base.AddSavingRegister(registers, register);
            }
        }

        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            var rangeOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderBy(v => v.Range)
                .ThenBy(v => v.Usages.Count).ToList();
            foreach (var variable in rangeOrdered) {
                if (variable.Type.ByteCount == 1 && !Conflict(variable.Intersections, ByteRegister.A) && CanAllocate(variable, ByteRegister.A)) {
                    variable.Register = ByteRegister.A;
                    continue;
                }
                if (variable.Type.ByteCount != 2)
                    continue;

                if (variable.Type is PointerType pointerType) {
                    if (pointerType.ElementType is StructureType) {
                        if (!Conflict(variable.Intersections, PointerRegister.Ix)) {
                            variable.Register = PointerRegister.Ix;
                        }
                        else if (!Conflict(variable.Intersections, PointerRegister.Iy)) {
                            variable.Register = PointerRegister.Iy;
                        }
                    }
                    else {
                        if (!Conflict(variable.Intersections, PointerRegister.Hl)) {
                            variable.Register = PointerRegister.Hl;
                        }
                    }
                }
                else {
                    if (!Conflict(variable.Intersections, WordRegister.Hl)) {
                        variable.Register = WordRegister.Hl;
                    }
                }
            }
            var usageOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
            foreach (var variable in usageOrdered) {
                var variableType = variable.Type;
                Register? register;
                if (variableType.ByteCount == 1) {
                    register = AllocatableRegister(variable, ByteRegister.Registers, function);
                }
                else {
                    if (variableType is PointerType pointerType) {
                        var registers = pointerType.ElementType is StructureType ? new List<PointerRegister>() { PointerRegister.Ix, PointerRegister.Iy, PointerRegister.Hl, PointerRegister.De, PointerRegister.Bc, } : new List<PointerRegister>() { PointerRegister.Hl, PointerRegister.De, PointerRegister.Bc, PointerRegister.Ix, PointerRegister.Iy };
                        register = AllocatableRegister(variable, registers, function);
                    }
                    else {
                        var registers = new List<WordRegister>() { WordRegister.Hl, WordRegister.De, WordRegister.Bc };
                        register = AllocatableRegister(variable, registers, function);
                    }
                }
                if (register == null)
                    continue;
                variable.Register = register;
            }

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
                else if (register is WordRegister wordRegister) {
                    if ((variable.Type is PointerType { ElementType: StructureType _ }) || Conflict(variable.Intersections, wordRegister)) {
                        register = AllocatableRegister(variable, WordRegister.Registers, function);
                        if (register != null) {
                            variable.Register = register;
                        }
                    }
                    else {
                        variable.Register = wordRegister;
                        break;
                    }
                }
                else if (register is PointerRegister pointerRegister) {
                    if ((variable.Type is PointerType { ElementType: StructureType _ }) || Conflict(variable.Intersections, pointerRegister)) {
                        List<Cate.PointerRegister> candidates;
                        if (variable.Type is PointerType pointerType) {
                            candidates = PointerRegister.PointerOrder(pointerType.ElementType is StructureType ? 10 : 0);
                        }
                        else {
                            throw new NotImplementedException();
                            //candidates = PointerRegister.Registers;
                        }
                        register = AllocatableRegister(variable, candidates, function);
                        if (register != null) {
                            variable.Register = register;
                        }
                    }
                    else {
                        variable.Register = pointerRegister;
                        break;
                    }
                }
            }
        }

        private static Register? AllocatableRegister<T>(Variable variable, IEnumerable<T> registers, Function function) where T : Register
        {
            return registers.FirstOrDefault(register => !Conflict(variable.Intersections, register) && CanAllocate(variable, register));
        }

        private static bool CanAllocate(Variable variable, Register register)
        {
            var function = variable.Block.Function;
            Debug.Assert(function != null);
            var first = variable.Usages.First().Key;
            var last = variable.Usages.Last().Key;
            for (var address = first; address <= last; ++address) {
                var instruction = function.Instructions[address];
                if (!instruction.CanAllocateRegister(variable, register))
                    return false;
            }
            return true;
        }

        private static bool Conflict<T>(IEnumerable<Variable> variables, T register) where T : Register
        {
            return variables.Any(v =>
                v.Register != null && register.Conflicts(v.Register));
        }

        public override Register? ParameterRegister(int index, ParameterizableType type)
        {
            return SubroutineInstruction.ParameterRegister(index, type);
        }

        public override Register? ReturnRegister(ParameterizableType type)
        {
            return SubroutineInstruction.ReturnRegister(type);
        }

        public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId,
            AssignableOperand destinationOperand, Operand leftOperand,
            Operand rightOperand)
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
                    if (destinationOperand.Type is PointerType)
                        return new PointerAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                    return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case '-': {
                        if (rightOperand is IntegerOperand { IntegerValue: > 0 } integerOperand) {
                            var operand = new IntegerOperand(rightOperand.Type, -integerOperand.IntegerValue);
                            if (destinationOperand.Type is PointerType)
                                return new PointerAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand, operand);
                            return new WordAddOrSubtractInstruction(function, '+', destinationOperand, leftOperand, operand);
                        }
                        if (destinationOperand.Type is PointerType)
                            return new PointerAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
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

        public override Cate.ResizeInstruction CreateResizeInstruction(Function function,
            AssignableOperand destinationOperand,
            IntegerType destinationType, Operand sourceOperand,
            IntegerType sourceType)
        {
            return new ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType);
        }

        public override Cate.CompareInstruction CreateCompareInstruction(Function function, int operatorId,
            Operand leftOperand,
            Operand rightOperand,
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
            AssignableOperand? destinationOperand, List<Operand> sourceOperands)
        {
            return new SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands);
        }

        public override Cate.ReturnInstruction CreateReturnInstruction(Function function, Operand? sourceOperand,
            Anchor anchor)
        {
            return new ReturnInstruction(function, sourceOperand, anchor);
        }

        public override Cate.DecrementJumpInstruction CreateDecrementJumpInstruction(Function function,
            AssignableOperand operand,
            Anchor anchor)
        {
            return new DecrementJumpInstruction(function, operand, anchor);
        }

        public override ReadOnlySpan<char> EndOfFunction => "\tret";

        public override Cate.MultiplyInstruction
            CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand,
                int rightValue)
        {
            return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
        }

        public override IEnumerable<Register> IncludedRegisters(Register? register)
        {
            if (register is WordRegister wordRegister) {
                return wordRegister.ByteRegisters;
            }
            return new List<Register>();
        }

        //public override int RegisterSize(int id)
        //{
        //    return id <= ByteRegister.L.Id ? 1 : 2;
        //}

        public override void CallExternal(Instruction instruction, string externalName)
        {
            instruction.WriteLine("\tcall\t" + externalName);
            Instance.AddExternalName(externalName);
        }

        public static bool IsHlFree(Instruction instruction, VariableOperand excludedVariableOperand)
        {
            return !instruction.IsRegisterReserved(WordRegister.Hl); //&&
            //!instruction.IsRegisterInVariableRange(WordRegister.Hl.Id, excludedVariableOperand.Variable);
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
    }
}
