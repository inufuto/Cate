using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate.MuCom87
{
    internal abstract class ByteOperation : Cate.ByteOperation
    {
        public override List<Cate.ByteRegister> Registers => ByteRegister.RegistersOtherThan(ByteRegister.A);
        public override List<Cate.ByteRegister> Accumulators => new List<Cate.ByteRegister>() { ByteRegister.A };


        protected override void OperateConstant(Instruction instruction, string operation, string value, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + value);
            }
        }

        protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset, int count)
        {
            var address = variable.MemoryAddress(offset);
            var register = instruction.GetVariableRegister(variable, offset);
            if (Equals(register, ByteRegister.A)) {
                ByteRegister.A.Operate(instruction, operation, change, count);
                ByteRegister.A.StoreToMemory(instruction, variable, offset);
                return;
            }

            using (var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers)) {
                var wordRegister = reservation.WordRegister;
                wordRegister.LoadFromMemory(instruction, address);
                for (var i = 0; i < count; ++i) {
                    Debug.Assert(wordRegister.High != null);
                    instruction.WriteLine("\t" + operation + "x\t" + wordRegister.AsmName);
                }
            }
            if (change) {
                instruction.RemoveVariableRegister(variable, offset);
            }
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.WordRegister pointerRegister, int offset,
            int count)
        {
            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadFromMemory(instruction, pointerRegister.AsmName);
                ByteRegister.A.Operate(instruction, operation, change, count);
                ByteRegister.A.StoreToMemory(instruction, pointerRegister.AsmName);
            }
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister,
            int offset, int value)
        {
            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadConstant(instruction, value);
                ByteRegister.A.StoreIndirect(instruction, pointerRegister, offset);
            }
        }

        public override void ClearByte(Instruction instruction, string label)
        {
            instruction.RemoveRegisterAssignment(ByteRegister.A);
            instruction.WriteLine("\txra\ta,a");
            instruction.WriteLine("\tmov\t" + label + ",a");
            instruction.AddChanged(ByteRegister.A);
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
        {
            //throw new System.NotImplementedException();
            const string label = MuCom87.Compiler.TemporaryByte;
            register.StoreToMemory(instruction, label);
            return label;
        }
    }
}
