using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            Cate.Compiler.Instance.WordOperation.UsingAnyRegister(instruction, WordRegister.Registers, wordRegister =>
            {
                wordRegister.LoadFromMemory(instruction, address);
                for (var i = 0; i < count; ++i) {
                    Debug.Assert(wordRegister.High != null);
                    instruction.WriteLine("\t" + operation + "x\t" + wordRegister.High.Name);
                }
            });
            if (change) {
                instruction.RemoveVariableRegister(variable, offset);
            }
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.WordRegister pointerRegister, int offset,
            int count)
        {
            UsingRegister(instruction, ByteRegister.A, () =>
             {
                 ByteRegister.A.LoadFromMemory(instruction, pointerRegister.Name);
                 ByteRegister.A.Operate(instruction, operation, change, count);
                 ByteRegister.A.StoreToMemory(instruction, pointerRegister.Name);
             });
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister,
            int offset, int value)
        {
            switch (pointerRegister) {
                case WordRegister wordRegister when offset == 0:
                    //TODO
                    break;
                case WordRegister wordRegister:
                    wordRegister.TemporaryOffset(instruction, offset, () =>
                    {
                        StoreConstantIndirect(instruction, wordRegister, 0, value);
                    });
                    return;
            }
            throw new NotImplementedException();
        }

        public override void ClearByte(Instruction instruction, string label)
        {
            instruction.RemoveRegisterAssignment(ByteRegister.A);
            instruction.WriteLine("\txra\ta,a");
            instruction.WriteLine("\tmov\t" + label + ",a");
            instruction.ChangedRegisters.Add(ByteRegister.A);
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
        {
            //throw new System.NotImplementedException();
            var label = Compiler.TemporaryByte;
            register.StoreToMemory(instruction, label);
            return label;
        }
    }
}
