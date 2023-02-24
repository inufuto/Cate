using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.I8086
{
    internal class Compiler : Cate.Compiler
    {
        public Compiler() : base(new ByteOperation(), new WordOperation())
        { }

        public override ISet<Register> SavingRegisters(Register register)
        {
            return new HashSet<Register>() { SavingRegister(register) };
        }

        private static Register SavingRegister(Register register)
        {
            //if (!(register is ByteRegister byteRegister) || Equals(byteRegister, ByteRegister.Ah) ||
            //    Equals(byteRegister, ByteRegister.Al)) return register;
            if (!(register is ByteRegister byteRegister)) {
                return register;
            }
            Debug.Assert(byteRegister.PairRegister != null);
            return byteRegister.PairRegister;
        }


        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            void Allocate1(List<Variable> list)
            {
                foreach (var variable in list) {
                    var variableType = variable.Type;
                    Register? register;
                    if (variableType.ByteCount == 1) {
                        register = AllocatableRegister(variable, ByteRegister.Registers, function);
                    }
                    else {
                        List<Cate.WordRegister> registers;
                        if (variableType is PointerType pointerType) {
                            registers = WordRegister.PointerOrder;
                        }
                        else {
                            registers = WordRegister.Registers;
                        }
                        register = AllocatableRegister(variable, registers, function);
                    }
                    if (register == null)
                        continue;
                    variable.Register = register;
                }
            }

            var rangeOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderBy(v => v.Range)
                .ThenBy(v => v.Usages.Count).ToList();

            Allocate1(rangeOrdered);
            var usageOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
            Allocate1(usageOrdered);

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
                        List<Cate.WordRegister> candidates;
                        if (variable.Type is PointerType) {
                            candidates = WordRegister.PointerRegisters;
                        }
                        else {
                            candidates = WordRegister.Registers;
                        }
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
            switch (operatorId) {
                case '|':
                case '^':
                case '&':
                    return new BitInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case '+':
                case '-':
                    return new AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case Keyword.ShiftLeft:
                case Keyword.ShiftRight:
                    return new ShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                default:
                    throw new NotImplementedException();
            }
        }

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

        public override ReadOnlySpan<char> EndOfFunction => "\tret";

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
            instruction.WriteLine("\tcall\t" + functionName);
            Instance.AddExternalName(functionName);
        }

        public override int Alignment => 2;

        //public override void RemoveSavingRegister(ISet<Register> savedRegisterIds, int byteCount)
        //{
        //    //if (byteCount == 1 && savedRegisterIds.Contains(WordRegister.Ax)) {
        //    //    savedRegisterIds.Remove(WordRegister.Ax);
        //    //    savedRegisterIds.Add(ByteRegister.Ah);
        //    //}
        //}

        protected override void RestoreRegister(StreamWriter writer, Register register, int byteCount)
        {
            if (Equals(register, WordRegister.Ax) && byteCount == 1) {
                writer.WriteLine("\tmov [@Temporary@Byte],al");
                register.Restore(writer, null, false, 0);
                writer.WriteLine("\tmov al,[@Temporary@Byte]");
            }
            else {
                register.Restore(writer, null, false, 0);
            }
        }

        protected override void WriteAssembly(StreamWriter writer)
        {
            writer.WriteLine("\textrn @Temporary@Byte");
            base.WriteAssembly(writer);
        }
    }
}
