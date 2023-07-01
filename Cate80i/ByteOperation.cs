using System.Collections.Generic;

namespace Inu.Cate.I8080
{
    internal class ByteOperation : Cate.ByteOperation
    {
        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
        public override List<Cate.ByteRegister> Accumulators => ByteRegister.Accumulators;

        //private Cate.WordOperation WordOperation => Cate.Compiler.Instance.WordOperation;

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

            if (!instruction.IsRegisterReserved(WordRegister.Hl)) {
                using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
                    WordRegister.Hl.LoadConstant(instruction, address);
                    for (var i = 0; i < count; ++i) {
                        instruction.WriteLine("\t" + operation + "\tm");
                    }
                }
                if (change) {
                    instruction.RemoveVariableRegister(variable, offset);
                }
                goto end;
            }

            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadFromMemory(instruction, address);
                OperateA();
            }
        end:
            ;
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.PointerRegister pointerRegister, int offset, int count)
        {
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    for (var i = 0; i < count; ++i) {
                        instruction.WriteLine("\t" + operation + "\tm");
                    }
                    return;
                }
                using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                    PointerRegister.Hl.CopyFrom(instruction, pointerRegister);
                    OperateIndirect(instruction, operation, change, PointerRegister.Hl, offset, count);
                }
                return;
            }
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                OperateIndirect(instruction, operation, change, pointerRegister, 0, count);
            });
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Variable pointer, int offset, int count)
        {
            if (Equals(pointer.Register, PointerRegister.Hl)) {
                OperateIndirect(instruction, operation, change, PointerRegister.Hl, offset, count);
                return;
            }
            using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                PointerRegister.Hl.LoadFromMemory(instruction, pointer, 0);
                OperateIndirect(instruction, operation, change, PointerRegister.Hl, offset, count);
            }
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset, int value)
        {
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    instruction.WriteLine("\tmvi\tm," + value);
                    return;
                }
                using (ReserveRegister(instruction, ByteRegister.A)) {
                    ByteRegister.A.LoadConstant(instruction, value);
                    instruction.WriteLine("\tstax\t" + pointerRegister);
                }
                return;
            }
            if (Equals(pointerRegister, PointerRegister.Hl)) {
                pointerRegister.TemporaryOffset(instruction, offset, () =>
                {
                    StoreConstantIndirect(instruction, pointerRegister, 0, value);
                });
                return;
            }
            using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                PointerRegister.Hl.CopyFrom(instruction, pointerRegister);
                StoreConstantIndirect(instruction, PointerRegister.Hl, offset, value);
            }
        }

        public override void ClearByte(Instruction instruction, string label)
        {
            instruction.RemoveRegisterAssignment(ByteRegister.A);
            instruction.WriteLine("\txra\ta");
            instruction.WriteLine("\tsta\t" + label);
            instruction.AddChanged(ByteRegister.A);
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
        {
            const string temporaryByte = I8080.Compiler.TemporaryByte;
            register.StoreToMemory(instruction, temporaryByte);
            return temporaryByte;
        }
    }
}
