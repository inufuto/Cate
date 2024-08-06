using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.MuCom87
{
    internal abstract class Compiler : Cate.Compiler
    {
        public const string TemporaryByte = "@TemporaryByte";
        public const string TemporaryWord = "@TemporaryWord";

        protected Compiler(Cate.ByteOperation byteOperation, Cate.WordOperation wordOperation,
            Cate.PointerOperation pointerOperation) : base(byteOperation, wordOperation, pointerOperation) { }


        protected override void WriteAssembly(StreamWriter writer)
        {
            writer.WriteLine("\text " + TemporaryByte);
            writer.WriteLine("\text " + SubroutineInstruction.TemporaryByte);
            writer.WriteLine("\text " + TemporaryWord);
            base.WriteAssembly(writer);
        }

        public override void AddSavingRegister(ISet<Register> registers, Register register)
        {
            if (register is ByteRegister byteRegister && byteRegister.PairRegister != null) {
                base.AddSavingRegister(registers, byteRegister.PairRegister);
            }
            else {
                base.AddSavingRegister(registers, register);
            }
        }

        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            var rangeOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderByDescending(v => v.Type.ByteCount).ThenBy(v => v.Range)
                .ThenBy(v => v.Usages.Count).ToList();
            var usageOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderByDescending(v => v.Type.ByteCount).ThenByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
            var byteRegisters = ByteOperation.RegistersOtherThan(ByteRegister.A);
            foreach (var variable in usageOrdered) {
                var register = variable.Type.ByteCount switch
                {
                    1 => AllocatableRegister(variable, byteRegisters, function),
                    _ => variable.Type switch
                    {
                        PointerType => AllocatableRegister(variable, PointerOperation.Registers, function),
                        _ => AllocatableRegister(variable, WordOperation.Registers, function)
                    }
                };

                if (register == null)
                    continue;
                variable.Register = register;
            }
            foreach (var variable in variables.Where(v => v.Register == null && !v.Static)) {
                if (variable.Parameter?.Register == null) {
                    continue;
                }
                var register = variable.Parameter.Register;
                if (
                    register is ByteRegister byteRegister &&
                    !Equals(byteRegister, ByteRegister.A) &&
                    !Conflict(variable.Intersections, byteRegister)
                ) {
                    variable.Register = byteRegister;
                }
                else if (register is ByteRegister) {
                    register = AllocatableRegister(variable, byteRegisters, function);
                    if (register != null) {
                        variable.Register = register;
                    }
                }
                else if (register is WordRegister wordRegister) {
                    if ((variable.Type is PointerType { ElementType: StructureType _ }) || Conflict(variable.Intersections, wordRegister)) {
                        var candidates = WordOperation.Registers;
                        register = AllocatableRegister(variable, candidates, function);
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
                        var candidates = PointerOperation.Registers;
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

        //private static bool Conflict<T>(IEnumerable<Variable> variables, T register) where T : Register
        //{
        //    return variables.Any(v =>
        //        v.Register != null && register.Conflicts(v.Register));
        //}

        //private static bool CanAllocate(Variable variable, Register register)
        //{
        //    var function = variable.Block.Function;
        //    Debug.Assert(function != null);
        //    var first = variable.Usages.First().Key;
        //    var last = variable.Usages.Last().Key;
        //    for (var address = first; address <= last; ++address) {
        //        var instruction = function.Instructions[address];
        //        if (!instruction.CanAllocateRegister(variable, register))
        //            return false;
        //    }
        //    return true;
        //}

        //private static Register? AllocatableRegister<T>(Variable variable, IEnumerable<T> registers, Function function) where T : Register
        //{
        //    return registers.FirstOrDefault(register => !Conflict(variable.Intersections, register) && CanAllocate(variable, register));
        //}


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

        public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand)
        {
            if (destinationOperand.Type.ByteCount == 1) {
                return operatorId switch
                {
                    '|' or '^' or '&' or '+' or '-' => new ByteBinomialInstruction(function, operatorId,
                        destinationOperand, leftOperand, rightOperand),
                    Keyword.ShiftLeft or Keyword.ShiftRight => CreateByteShiftInstruction(function, operatorId,
                        destinationOperand, leftOperand, rightOperand),
                    _ => throw new NotImplementedException()
                };
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

        protected abstract ByteShiftInstruction CreateByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand);

        public override Cate.MonomialInstruction CreateMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return new MonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
        }

        public override Cate.ResizeInstruction CreateResizeInstruction(Function function, AssignableOperand destinationOperand,
            IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
        {
            return new ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType);
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

        public override ReadOnlySpan<char> EndOfFunction => "\tret";

        public override Cate.MultiplyInstruction CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand,
            Operand leftOperand, int rightValue)
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
                        case Cate.WordPointerRegister pointerRegister:
                            Debug.Assert(pointerRegister.Low != null);
                            return new ByteRegisterOperand(newType, pointerRegister.Low);
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
                        case Cate.WordPointerRegister pointerRegister:
                            Debug.Assert(pointerRegister.High != null);
                            return new ByteRegisterOperand(newType, pointerRegister.High);
                        default:
                            return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                    }
                case IndirectOperand indirectOperand:
                    return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset + 1);
                default:
                    throw new NotImplementedException();
            }
        }

        public override void CallExternal(Instruction instruction, string externalName)
        {
            instruction.WriteLine("\tcall\t" + externalName);
            Instance.AddExternalName(externalName);
        }

        public abstract void SkipIfZero(Instruction instruction);
    }
}
