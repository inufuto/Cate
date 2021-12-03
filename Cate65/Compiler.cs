using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate
{
}

namespace Inu.Cate.Mos6502
{
    internal class Compiler : Cate.Compiler
    {
        public const int RegisterA = 1;
        public const string ZeroPageLabel = "@zp";

        public Compiler() : base(new ByteOperation(), new WordOperation()) { }

        protected override void WriteAssembly(StreamWriter writer)
        {
            //writer.WriteLine("\tinclude\t'Cate6502.inc'");
            writer.WriteLine("extrn " + ZeroPageLabel);
            base.WriteAssembly(writer);
        }


        public override ISet<Register> SavingRegisters(Register register)
        {
            return new HashSet<Register>() { register };
        }


        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            var shortRange = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null && v.Range <= 1).OrderBy(v => v.Usages.Count).ToList();
            foreach (var variable in shortRange) {
                if (variable.Type.ByteCount != 1 || Conflict(variable.Intersections, ByteRegister.A) ||
                    !CanAllocate(variable, ByteRegister.A))
                    continue;
                variable.Register = ByteRegister.A;
            }
            var rangeOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderBy(v => v.Range)
                .ThenBy(v => v.Usages.Count);
            foreach (var variable in rangeOrdered.Where(v => v.Range <= 1)) {
                if (variable.Type.ByteCount != 1 || Conflict(variable.Intersections, ByteRegister.X) ||
                    !CanAllocate(variable, ByteRegister.X))
                    continue;
                variable.Register = ByteRegister.X;
            }

            var usageOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
            foreach (var variable in usageOrdered) {
                var variableType = variable.Type;
                var register = variableType.ByteCount == 1 ? AllocatableRegister(variable, ByteZeroPage.Registers, function) : AllocatableRegister(variable, WordZeroPage.Registers, function);
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
                    register = AllocatableRegister(variable, ByteZeroPage.Registers, function);
                    if (register != null) {
                        variable.Register = register;
                    }
                }
                else if (register is WordRegister wordRegister) {
                    if ((variable.Type is PointerType { ElementType: StructureType _ }) || Conflict(variable.Intersections, wordRegister)) {
                        var candidates = WordZeroPage.Registers;
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
            }
        }

        private static Register? AllocatableRegister<T>(Variable variable, IEnumerable<T> registers, Function function) where T : Register
        {
            foreach (var register in registers) {
                if (!Conflict(variable.Intersections, register) && CanAllocate(variable, register))
                    return register;
            }
            return null;
        }

        private static bool CanAllocate(Variable variable, Register register)
        {
            var function = variable.Block.Function;
            Debug.Assert(function != null);
            var first = variable.Usages.First().Key;
            var last = variable.Usages.Last().Key;
            for (var address = first; address <= last; ++address) {
                var instruction = function.Instructions[address];
                if (!instruction.CanAllocateRegister(variable, register)) {
                    return false;
                }
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

        public override Register ReturnRegister(int byteCount)
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

        public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId,
            AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand)
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
                case '|':
                case '^':
                case '&':
                    return new WordBinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case '+':
                case '-':
                    return new WordBinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case Keyword.ShiftLeft:
                case Keyword.ShiftRight:
                    return new WordShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                default:
                    throw new NotImplementedException();
            }
        }

        public override Cate.MonomialInstruction CreateMonomialInstruction(Function function, int operatorId,
            AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return new MonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
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

        public override IEnumerable<Register> IncludedRegisterIds(Register register)
        {
            return new List<Register>() { register };
        }

        public override bool IsAssignedRegisterPrior() => true;


        public override Operand HighByteOperand(Operand operand)
        {
            var newType = ToByteType(operand);
            switch (operand) {
                case IntegerOperand integerOperand:
                    return new StringOperand(newType, "high " + integerOperand.IntegerValue);
                case PointerOperand pointerOperand:
                    return new StringOperand(newType, "high(" + pointerOperand.MemoryAddress() + ")");
                case VariableOperand variableOperand:
                    if (variableOperand.Register is WordRegister wordRegister) {
                        Debug.Assert(wordRegister.High != null);
                        return new ByteRegisterOperand(newType, wordRegister.High);
                    }
                    else
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                case IndirectOperand indirectOperand:
                    return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset + 1);
                default:
                    throw new NotImplementedException();
            }
        }

        public override Operand LowByteOperand(Operand operand)
        {
            var newType = ToByteType(operand);
            switch (operand) {
                case IntegerOperand integerOperand:
                    return new StringOperand(newType, "low " + integerOperand.IntegerValue);
                case PointerOperand pointerOperand:
                    return new StringOperand(newType, "low(" + pointerOperand.MemoryAddress() + ")");
                case VariableOperand variableOperand:
                    if (variableOperand.Register is WordRegister wordRegister) {
                        Debug.Assert(wordRegister.Low != null);
                        return new ByteRegisterOperand(newType, wordRegister.Low);
                    }
                    else {
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset);
                    }
                case IndirectOperand indirectOperand:
                    return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset);
            }
            throw new NotImplementedException();
        }

        public override void CallExternal(Instruction instruction, string externalName)
        {
            instruction.WriteLine("\tjsr\t" + externalName);
            Instance.AddExternalName(externalName);
        }

    }
}
