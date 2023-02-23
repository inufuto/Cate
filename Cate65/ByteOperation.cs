using System;
using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.Mos6502
{
    internal class ByteOperation : Cate.ByteOperation
    {
        public override List<Cate.ByteRegister> Accumulators => new List<Cate.ByteRegister>() { ByteRegister.A };
        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers.Union(ByteZeroPage.Registers).ToList();

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
            instruction.RemoveVariableRegister(variable, offset);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change,
            WordRegister pointerRegister, int offset, int count)
        {
            if (!(pointerRegister is WordZeroPage zeroPage)) {
                throw new NotImplementedException();
            }
            while (true) {
                if (pointerRegister.IsOffsetInRange(offset)) {
                    ByteRegister.Y.LoadConstant(instruction, offset);
                    for (var i = 0; i < count; ++i) {
                        instruction.WriteLine("\t" + operation + "\t(" + zeroPage.Name + "),y");
                    }
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    return;
                }
                pointerRegister.Add(instruction, offset);
                offset = 0;
            }
        }

        public override void StoreConstantIndirect(Instruction instruction, WordRegister pointerRegister, int offset,
            int value)
        {
            UsingAnyRegister(instruction, register =>
            {
                register.LoadConstant(instruction,value);
                //instruction.WriteLine("\tld" + register + "\t#" + value);
                //instruction.SetRegisterConstant(register, value);
                register.StoreIndirect(instruction, pointerRegister, offset);
            });
        }

        public override void ClearByte(Instruction instruction, string label)
        {
            UsingAnyRegister(instruction, register =>
            {
                register.LoadConstant(instruction, 0);
                instruction.RemoveRegisterAssignment(register);
                register.StoreToMemory(instruction, label);
            });
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
        {
            var label = ByteZeroPage.TemporaryByte;
            register.StoreToMemory(instruction, label);
            return label;
        }
    }
}