using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Mc6800
{
    internal class ByteOperation : Cate.ByteOperation
    {
        public static string ByteOperand(Instruction instruction, Operand operand)
        {
            return operand switch
            {
                IntegerOperand integerOperand => "#" + integerOperand.IntegerValue,
                StringOperand stringOperand => stringOperand.StringValue,
                VariableOperand variableOperand => variableOperand.MemoryAddress(),
                _ => throw new NotImplementedException()
            };
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister,
            int offset, int value)
        {
            UsingAnyRegister(instruction, register =>
            {
                register.LoadConstant(instruction, value);
                register.StoreIndirect(instruction, pointerRegister, offset);
            });
        }

        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;

        public override List<Cate.ByteRegister> Accumulators => Registers;

        protected override void OperateConstant(Instruction instruction, string operation, string value, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t#" + value);
            }
        }

        protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable,
            int offset, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + variable.MemoryAddress(offset));
            }
            if (change) {
                instruction.RemoveVariableRegister(variable, offset);
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change,
            Cate.WordRegister pointerRegister, int offset, int count)
        {
            Debug.Assert(Equals(pointerRegister, WordRegister.X));
            while (true) {
                if (pointerRegister.IsOffsetInRange(offset)) {
                    for (var i = 0; i < count; ++i) {
                        instruction.WriteLine("\t" + operation + "\t" + offset + "," + pointerRegister);
                    }
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    return;
                }
                pointerRegister.Add(instruction, offset);
                offset = 0;
            }
        }

        public override void ClearByte(Instruction instruction, string label)
        {
            instruction.WriteLine("\tclr\t" + label);
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister rightRegister)
        {
            rightRegister.StoreToMemory(instruction, ZeroPage.Byte.Name);
            return ZeroPage.Byte.ToString();
        }
    }
}