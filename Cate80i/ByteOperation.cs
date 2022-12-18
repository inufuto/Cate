using System;
using System.Collections.Generic;

namespace Inu.Cate.I8080
{
    internal class ByteOperation : Cate.ByteOperation
    {
        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
        public override List<Cate.ByteRegister> Accumulators => ByteRegister.Accumulators;

        private Cate.WordOperation WordOperation => Cate.Compiler.Instance.WordOperation;

        protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset, int count)
        {
            var address = variable.MemoryAddress(offset);

            void OperateA()
            {
                ByteRegister.A.Operate(instruction, operation, change, count);
                ByteRegister.A.StoreToMemory(instruction, variable, offset);
            }

            var register = instruction.GetVariableRegister(variable, offset);
            if (Equals(register, ByteRegister.A)) {
                OperateA();
                goto end;
            }

            if (!instruction.IsRegisterInUse(WordRegister.Hl)) {
                instruction.BeginRegister(WordRegister.Hl);
                WordRegister.Hl.LoadConstant(instruction, address);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\tm");
                }
                instruction.EndRegister(WordRegister.Hl);
                if (change) {
                    instruction.RemoveVariableRegister(variable, offset);
                }
                goto end;
            }
            UsingRegister(instruction, ByteRegister.A, () =>
            {
                ByteRegister.A.LoadFromMemory(instruction, address);
                OperateA();
            });
        end:
            ;
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.WordRegister pointerRegister, int offset, int count)
        {
            if (offset == 0) {
                if (Equals(pointerRegister, WordRegister.Hl)) {
                    for (var i = 0; i < count; ++i) {
                        instruction.WriteLine("\t" + operation + "\tm");
                    }
                    return;
                }
                WordOperation.UsingRegister(instruction, WordRegister.Hl, () =>
                {
                    WordRegister.Hl.CopyFrom(instruction, pointerRegister);
                    OperateIndirect(instruction, operation, change, WordRegister.Hl, offset, count);
                });
                return;
            }
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                OperateIndirect(instruction, operation, change, pointerRegister, 0, count);
            });
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Variable pointer, int offset, int count)
        {
            if (Equals(pointer.Register, WordRegister.Hl)) {
                OperateIndirect(instruction, operation, change, WordRegister.Hl, offset, count);
                return;
            }
            Cate.Compiler.Instance.WordOperation.UsingRegister(instruction, WordRegister.Hl, () =>
            {
                WordRegister.Hl.LoadFromMemory(instruction, pointer, 0);
                OperateIndirect(instruction, operation, change, WordRegister.Hl, offset, count);
            });
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset, int value)
        {
            if (offset == 0) {
                if (Equals(pointerRegister, WordRegister.Hl)) {
                    instruction.WriteLine("\tmvi\tm," + value);
                    return;
                }
                UsingRegister(instruction, ByteRegister.A, () =>
                {
                    ByteRegister.A.LoadConstant(instruction, value);
                    instruction.WriteLine("\tstax\t" + pointerRegister);
                });
                return;
            }
            if (Equals(pointerRegister, WordRegister.Hl)) {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    StoreConstantIndirect(instruction, pointerRegister, 0, value);
                });
                return;
            }
            WordOperation.UsingRegister(instruction, WordRegister.Hl, () =>
            {
                WordRegister.Hl.CopyFrom(instruction, pointerRegister);
                StoreConstantIndirect(instruction, WordRegister.Hl, offset, value);
            });
        }

        public override void ClearByte(Instruction instruction, string label)
        {
            instruction.RemoveRegisterAssignment(ByteRegister.A);
            instruction.WriteLine("\txra\ta");
            instruction.WriteLine("\tsta\t" + label);
            instruction.ChangedRegisters.Add(ByteRegister.A);
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
        {
            register.StoreToMemory(instruction, Compiler.TemporaryByte);
            return Compiler.TemporaryByte;
        }
    }
}
