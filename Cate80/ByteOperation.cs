using System;
using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class ByteOperation : Cate.ByteOperation
    {
        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
        public override List<Cate.ByteRegister> Accumulators => ByteRegister.Accumulators;

        protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable,
            int offset, int count)
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
                WordRegister.Hl.LoadFromMemory(instruction, address);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "(hl)");
                }
                instruction.EndRegister(WordRegister.Hl);
                if (change) {
                    instruction.RemoveVariableRegister(variable, offset);
                }
                goto end;
            }
            ByteRegister.UsingAccumulator(instruction, () =>
            {
                ByteRegister.A.LoadFromMemory(instruction, address);
                OperateA();
            });
            end:
            ;
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change,
            Cate.WordRegister pointerRegister, int offset, int count)
        {
            if (pointerRegister.IsIndex() && pointerRegister.IsOffsetInRange(offset)) {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "(" + pointerRegister + "+" + offset + ")");
                }
                return;
            }
            if (offset == 0) {
                var operand = "(" + pointerRegister + ")";
                if (Equals(pointerRegister, WordRegister.Hl)) {
                    for (var i = 0; i < count; ++i) {
                        instruction.WriteLine("\t" + operation + operand);
                    }
                    return;
                }
                ByteRegister.UsingAccumulator(instruction, () =>
                {
                    ByteRegister.A.LoadFromMemory(instruction, operand);
                    ByteRegister.A.Operate(instruction, operation, change, count);
                    ByteRegister.A.StoreToMemory(instruction, operand);
                });
                return;
            }
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                OperateIndirect(instruction, operation, change, pointerRegister, 0, count);
            });
        }


        protected override void SaveAndRestore(Instruction instruction, Cate.ByteRegister register, Action action)
        {
            UsingAnyRegister(instruction, Registers.Where(r => !Equals(r, register)).ToList(), temporaryRegister =>
            {
                temporaryRegister.CopyFrom(instruction, register);
                action();
                register.CopyFrom(instruction, temporaryRegister);
            });
        }

        public override void ClearByte(Instruction instruction, string label)
        {
            instruction.RemoveRegisterAssignment(ByteRegister.A);
            instruction.WriteLine("\txor\ta");
            instruction.WriteLine("\tld\t(" + label + "),a");
            instruction.ChangedRegisters.Add(ByteRegister.A);
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister,
            int offset, int value)
        {
            if (pointerRegister.IsIndex() && pointerRegister.IsOffsetInRange(offset)) {
                instruction.WriteLine("\tld\t(" + pointerRegister + "+" + offset + ")," + value);
                return;
            }
            if (offset == 0) {
                if (pointerRegister.IsAddable()) {
                    instruction.WriteLine("\tld\t(" + pointerRegister + ")," + value);
                    return;
                }
                ByteRegister.UsingAccumulator(instruction, () =>
                {
                    ByteRegister.A.LoadConstant(instruction, value);
                    instruction.WriteLine("\tld\t(" + pointerRegister + "),a");
                });
                return;
            }
            if (pointerRegister.IsAddable()) {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    StoreConstantIndirect(instruction, pointerRegister, 0, value);
                });
                return;
            }
            var candidates = WordRegister.PointerOrder(offset).Where(r => !Equals(r, pointerRegister)).ToList();
            WordRegister.UsingAny(instruction, candidates, temporaryRegister =>
            {
                temporaryRegister.CopyFrom(instruction, pointerRegister);
                StoreConstantIndirect(instruction, temporaryRegister, offset, value);
            });
        }


        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister rightRegister)
        {
            throw new NotImplementedException();
        }
    }
}
