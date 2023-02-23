using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.I8080
{
    internal class Compiler : Cate.Compiler
    {
        public const string TemporaryByte = "@Temporary@Byte";

        public Compiler() : base(new ByteOperation(), new WordOperation()) { }

        protected override void WriteAssembly(StreamWriter writer)
        {
            writer.Write("\text\t" + TemporaryByte);
            base.WriteAssembly(writer);
        }

        public override ISet<Register> SavingRegisters(Register register)
        {
            return new HashSet<Register>() { SavingRegister(register) };
        }

        private static Register SavingRegister(Register register)
        {
            if (Equals(register, ByteRegister.A))
                return register;
            if (!(register is ByteRegister byteRegister))
                return register;
            Debug.Assert(byteRegister.PairRegister != null);
            return byteRegister.PairRegister;
        }

        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            void AllocateOrdered(List<Variable> ordered)
            {
                foreach (var variable in ordered) {
                    var variableType = variable.Type;
                    Register? register;
                    if (variableType.ByteCount == 1)
                    {
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

            var rangeOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderBy(v => v.Range)
                .ThenBy(v => v.Usages.Count).ToList();
            AllocateOrdered(rangeOrdered);
            //foreach (var variable in rangeOrdered) {
            //    if (variable.Type.ByteCount == 1 && !Conflict(variable.Intersections, ByteRegister.A) && CanAllocate(variable, ByteRegister.A)) {
            //        variable.Register = ByteRegister.A;
            //        continue;
            //    }
            //    if (variable.Type.ByteCount != 2)
            //        continue;
            //    if (!Conflict(variable.Intersections, WordRegister.Hl)) {
            //        variable.Register = WordRegister.Hl;
            //    }
            //}
            var usageOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
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

        public override Register? ReturnRegister(int byteCount)
        {
            return SubroutineInstruction.ReturnRegister(byteCount);
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
            return destinationOperand.Type.ByteCount switch
            {
                // + - ~
                1 => new ByteMonomialInstruction(function, operatorId, destinationOperand, sourceOperand),
                _ => new WordMonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
            };
        }

        public override Cate.ResizeInstruction CreateResizeInstruction(Function function, AssignableOperand destinationOperand,
            IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
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

        public override Cate.SubroutineInstruction CreateSubroutineInstruction(Function function, Function targetFunction,
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

        public override Cate.MultiplyInstruction CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand,
            Operand leftOperand, int rightValue)
        {
            return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
        }

        public override IEnumerable<Register> IncludedRegisterIds(Register register)
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
                    if (!(variableOperand.Register is Cate.WordRegister wordRegister))
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset);
                    Debug.Assert(wordRegister.Low != null);
                    return new ByteRegisterOperand(newType, wordRegister.Low);
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
                    if (!(variableOperand.Register is Cate.WordRegister wordRegister))
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                    Debug.Assert(wordRegister.High != null);
                    return new ByteRegisterOperand(newType, wordRegister.High);
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
}
