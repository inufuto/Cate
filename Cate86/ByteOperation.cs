using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Inu.Cate.I8086
{
    internal class ByteOperation : Cate.ByteOperation
    {
        public override List<Cate.ByteRegister> Accumulators => ByteRegister.Registers;

        protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset, int count)
        {
            if (variable.Register != null) {
                Debug.Assert(offset == 0);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + variable.Register);
                }
                return;
            }
            var address = variable.MemoryAddress(offset);
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "byte ptr [" + address + "]");
            }
            instruction.RemoveVariableRegister(variable, offset);
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.WordRegister pointerRegister, int offset,
            int count)
        {
            if (!pointerRegister.IsPointer(offset)) {
                Cate.Compiler.Instance.WordOperation.UsingAnyRegister(instruction, WordRegister.PointerRegisters, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, pointerRegister);
                    OperateIndirect(instruction, operation, change, temporaryRegister, offset, count);
                });
                return;
            }
            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "byte ptr [" + WordRegister.AsPointer(pointerRegister) + addition + "]");
            }
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset, int value)
        {
            if (!pointerRegister.IsPointer(offset)) {
                Cate.Compiler.Instance.WordOperation.UsingAnyRegister(instruction, WordRegister.PointerRegisters, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, pointerRegister);
                    StoreConstantIndirect(instruction, temporaryRegister, offset, value);
                });
                return;
            }
            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
            instruction.WriteLine("\tmov byte ptr [" + WordRegister.AsPointer(pointerRegister) + addition + "]," + value);
        }

        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
        public override void ClearByte(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov byte ptr [" + label + "],0");
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister rightRegister)
        {
            throw new NotImplementedException();
        }
    }
}
