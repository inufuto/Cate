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

        protected Compiler(Cate.ByteOperation byteOperation, Cate.WordOperation wordOperation) : base(byteOperation, wordOperation) { }


        protected override void WriteAssembly(StreamWriter writer)
        {
            writer.WriteLine("\text " + TemporaryByte);
            writer.WriteLine("\text " + SubroutineInstruction.TemporaryByte);
            writer.WriteLine("\text " + TemporaryWord);
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
            if (register is ByteRegister byteRegister) {
                Debug.Assert(byteRegister.PairRegister != null);
                return byteRegister.PairRegister;
            }
            return register;
        }

        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            var rangeOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderByDescending(v => v.Type.ByteCount).ThenBy(v => v.Range)
                .ThenBy(v => v.Usages.Count).ToList();
            var usageOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderByDescending(v => v.Type.ByteCount).ThenByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
            var byteRegisters = ByteOperation.RegistersOtherThan(ByteRegister.A);
            foreach (var variable in usageOrdered) {
                var register = variable.Type.ByteCount == 1 ? AllocatableRegister(variable, byteRegisters, function) : AllocatableRegister(variable, WordOperation.Registers, function);
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
                    !Equals(byteRegister, ByteRegister.A) && // || accumulatorVariables.Count() <= 1)
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
                        List<Cate.WordRegister> candidates = WordOperation.Registers;
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

        private static bool Conflict<T>(IEnumerable<Variable> variables, T register) where T : Register
        {
            return variables.Any(v =>
                v.Register != null && register.Conflicts(v.Register));
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

        private static Register? AllocatableRegister<T>(Variable variable, IEnumerable<T> registers, Function function) where T : Register
        {
            return registers.FirstOrDefault(register => !Conflict(variable.Intersections, register) && CanAllocate(variable, register));
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
                    case '+':
                    case '-':
                        return new ByteBinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                    case Keyword.ShiftLeft:
                    case Keyword.ShiftRight:
                        return CreateByteShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
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

        public override IEnumerable<Register> IncludedRegisterIds(Register? register)
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
                    if (variableOperand.Register is Cate.WordRegister wordRegister) {
                        Debug.Assert(wordRegister.Low != null);
                        return new ByteRegisterOperand(newType, wordRegister.Low);
                    }
                    else {
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
                    if (variableOperand.Register is Cate.WordRegister wordRegister) {
                        Debug.Assert(wordRegister.High != null);
                        return new ByteRegisterOperand(newType, wordRegister.High);
                    }
                    else {
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

        //public override void SaveRegisters(StreamWriter writer, ISet<Register> registers)
        //{
        //    //var actualRegisters = ActualRegisters(registers, out List<ByteWorkingRegister> workingRegisters);
        //    base.SaveRegisters(writer, actualRegisters);

        //    if (!workingRegisters.Any()) return;
        //    workingRegisters.Sort((a, b) => a.Id - b.Id);
        //    writer.WriteLine("\tshld\t" + TemporaryWord);
        //    for (var i = 0; i < workingRegisters.Count; ++i) {
        //        var current = workingRegisters[i];
        //        writer.WriteLine("\tlhld\t" + current.Name + " | push h");
        //        if (i + 1 >= workingRegisters.Count) continue;
        //        var next = workingRegisters[i + 1];
        //        if (next.Id == current.Id + 1) {
        //            ++i;
        //        }
        //    }
        //    writer.WriteLine("\tlhld\t" + TemporaryWord);
        //}

        //public override void RestoreRegisters(StreamWriter writer, ISet<Register> registers)
        //{
        //    //var actualRegisters = ActualRegisters(registers, out List<ByteWorkingRegister> workingRegisters);
        //    if (workingRegisters.Any()) {
        //        workingRegisters.Sort((a, b) => a.Id - b.Id);
        //        writer.WriteLine("\tshld\t" + TemporaryWord);
        //        Stack<string> stack = new Stack<string>();
        //        for (var i = 0; i < workingRegisters.Count; ++i) {
        //            var current = workingRegisters[i];
        //            stack.Push("\tpop h | shld\t" + current.Name);
        //            if (i + 1 >= workingRegisters.Count) continue;
        //            var next = workingRegisters[i + 1];
        //            if (next.Id == current.Id + 1) {
        //                ++i;
        //            }
        //        }
        //        while (stack.Any()) {
        //            writer.WriteLine(stack.Pop());
        //        }
        //        writer.WriteLine("\tlhld\t" + TemporaryWord);
        //    }
        //    base.RestoreRegisters(writer, actualRegisters);
        //}

        //private static ISet<Register> ActualRegisters(ISet<Register> registers, out List<ByteWorkingRegister> workingRegisters)
        //{
        //    ISet<Register> actualRegisters = new HashSet<Register>();
        //    workingRegisters = new List<ByteWorkingRegister>();
        //    foreach (var register in registers) {
        //        if (register is ByteWorkingRegister byteWorkingRegister) {
        //            workingRegisters.Add(byteWorkingRegister);
        //        }
        //        else {
        //            actualRegisters.Add(register);
        //        }
        //    }
        //    return actualRegisters;
        //}
        public abstract void SkipIfZero(Instruction instruction);
    }
}
