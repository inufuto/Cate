﻿using System;
using System.Collections.Generic;

namespace Inu.Cate.Mc6809
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

        public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister,
            int offset, int value)
        {
            if (!Equals(pointerRegister, PointerRegister.D)) {
                using var reservation = ReserveAnyRegister(instruction);
                var register = reservation.ByteRegister;
                instruction.RemoveRegisterAssignment(register);
                register.LoadConstant(instruction, value);
                register.StoreIndirect(instruction, pointerRegister, offset);
                return;
            }
            using (var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.IndexRegisters)) {
                var temporaryRegister = reservation.PointerRegister;
                temporaryRegister.CopyFrom(instruction, pointerRegister);
                StoreConstantIndirect(instruction, temporaryRegister, offset, value);
            }
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
            Cate.PointerRegister pointerRegister, int offset, int count)
        {
            if (offset == 0) {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t," + pointerRegister);
                }
            }
            else {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t" + offset + "," + pointerRegister);
                }
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change,
            Variable pointer, int offset, int count)
        {
            if (offset == 0) {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t[" + pointer.MemoryAddress(0) + "]");
                }
                return;
            }
            base.OperateIndirect(instruction, operation, change, pointer, offset, count);
        }


        public override void ClearByte(Instruction instruction, string label)
        {
            instruction.WriteLine("\tclr\t" + label);
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister rightRegister)
        {
            instruction.WriteLine("\tst" + rightRegister + "\t" + DirectPage.Byte);
            return DirectPage.Byte.ToString();
        }
    }
}