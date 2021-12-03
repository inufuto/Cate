using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Inu.Cate.Mc6800
{
    internal class Compiler : Cate.Compiler
    {
        public Compiler() : base(new ByteOperation(), new WordOperation()) { }
        protected override void WriteAssembly(StreamWriter writer)
        {
            //writer.WriteLine("\tinclude\t'Temp6800.inc'");
            writer.WriteLine("extrn " + ZeroPage.Byte.Label);
            writer.WriteLine("extrn " + ZeroPage.Word.Label);
            writer.WriteLine("extrn " + ZeroPage.Word2.Label);
            base.WriteAssembly(writer);
        }


        public override ISet<Register> SavingRegisters(Register register)
        {
            return !Equals(register, WordRegister.X) ? new HashSet<Register>() { register } : new HashSet<Register>();
        }

        public override void AllocateRegisters(List<Variable> variables, Function function)
        {
            IEnumerable<Variable> TargetVariables() => variables.Where(v => v.Register == null && !v.Static && v.Type.ByteCount == 1);

            var rangeOrdered = TargetVariables().Where(v => v.Parameter == null).OrderBy(v => v.Range)
                .ThenByDescending(v => v.Usages.Count);
            foreach (var variable in rangeOrdered) {
                if (variable.Intersections.All(v => !Equals(v.Register, ByteRegister.B))) {
                    variable.Register = ByteRegister.B;
                }
            }
            var usageOrdered = TargetVariables().Where(v => v.Parameter == null).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
            foreach (var variable in usageOrdered.Where(variable => variable.Intersections.All(v => v.Register == null))) {
                variable.Register = ByteRegister.A;
            }
            foreach (var variable in TargetVariables()) {
                if (variable.Parameter?.Register != null) {
                    var register = variable.Parameter.Register;
                    if (variable.Intersections.All(v => !Equals(v.Register, register))) {
                        variable.Register = register;
                    }
                }
            }
        }


        public override Register? ParameterRegister(int index, ParameterizableType type)
        {
            return index == 0 && type.ByteCount == 1 ? ByteRegister.A : null;
        }

        public override Register ReturnRegister(int byteCount)
        {
            return byteCount == 1 ? (Register)ByteRegister.A : WordRegister.X;
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
                    return new WordBitInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case '+':
                case '-':
                    return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
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
                1 => new ByteMonomialInstruction(function, operatorId, destinationOperand, sourceOperand),
                _ => operatorId switch
                {
                    '-' => new WordNegateInstruction(function, operatorId, destinationOperand, sourceOperand),
                    '~' => new WordComplementInstruction(function, operatorId, destinationOperand, sourceOperand),
                    _ => throw new NotImplementedException()
                }
            };
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

        public override IEnumerable<Register> IncludedRegisterIds(Register returnRegisterId)
        {
            return new List<Register>();
        }

        public override Operand HighByteOperand(Operand operand)
        {
            var newType = ToByteType(operand);
            switch (operand) {
                case ConstantOperand constantOperand:
                    return new StringOperand(newType, "high(" + constantOperand.MemoryAddress() + ")");
                case VariableOperand variableOperand:
                    Debug.Assert(variableOperand.Register == null);
                    return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset);
                case IndirectOperand indirectOperand:
                    return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset);
                default:
                    throw new NotImplementedException();
            }
        }

        public override Operand LowByteOperand(Operand operand)
        {
            var newType = ToByteType(operand);
            switch (operand) {
                case ConstantOperand constantOperand:
                    return new StringOperand(newType, "low(" + constantOperand.MemoryAddress() + ")");
                case VariableOperand variableOperand:
                    Debug.Assert(variableOperand.Register == null);
                    return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                case IndirectOperand indirectOperand:
                    return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset + 1);
                default:
                    throw new NotImplementedException();
            }
        }



        public static string WordOperand(Operand operand)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    var value = integerOperand.IntegerValue;
                    return "#" + value;
                case PointerOperand pointerOperand:
                    return "#" + AssemblyString(pointerOperand.Variable.Label, pointerOperand.Offset);
                case VariableOperand variableOperand:
                    Debug.Assert(variableOperand.Variable.Register == null);
                    return variableOperand.MemoryAddress();
                case IndirectOperand indirectOperand:
                    return (indirectOperand.Offset & 0xff) + ",x";
                default:
                    throw new NotImplementedException();
            }
        }


        private static string AssemblyString(string label, int offset)
        {
            StringBuilder s = new StringBuilder(label);
            if (offset == 0)
                return s.ToString();
            s.Append('+');
            s.Append(offset.ToString());
            return s.ToString();
        }

        public override void CallExternal(Instruction instruction, string externalName)
        {
            instruction.WriteLine("\tjsr\t" + externalName);
            Instance.AddExternalName(externalName);
        }

        public static void LoadPairRegister(Instruction instruction, Operand operand)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    var value = integerOperand.IntegerValue;
                    ByteRegister.A.LoadConstant(instruction, "high " + value);
                    ByteRegister.B.LoadConstant(instruction, "low " + value);
                    return;
                case PointerOperand pointerOperand:
                    ByteRegister.A.LoadConstant(instruction, "high(" + pointerOperand.MemoryAddress()+")");
                    ByteRegister.B.LoadConstant(instruction, "low(" + pointerOperand.MemoryAddress()+")");
                    return;
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        ByteRegister.A.LoadFromMemory(instruction, variable, offset);
                        ByteRegister.B.LoadFromMemory(instruction, variable, offset + 1);
                        return;
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        Debug.Assert(pointer.Register == null);
                        WordRegister.X.LoadFromMemory(instruction, pointer, 0);
                        ByteRegister.A.LoadIndirect(instruction, WordRegister.X, offset);
                        ByteRegister.B.LoadIndirect(instruction, WordRegister.X, offset + 1);
                        return;
                    }
            }
            throw new NotImplementedException();
        }
    }
}
