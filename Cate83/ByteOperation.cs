namespace Inu.Cate.Sm83;

internal class ByteOperation : Cate.ByteOperation
{
    public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
    public override List<Cate.ByteRegister> Accumulators => ByteRegister.Accumulators;
    protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset, int count)
    {
        var address = variable.MemoryAddress(offset);

        var register = instruction.GetVariableRegister(variable, offset);
        if (Equals(register, ByteRegister.A)) {
            OperateA();
            goto end;
        }
        if (!instruction.IsRegisterReserved(PointerRegister.Hl)) {
            using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                PointerRegister.Hl.LoadConstant(instruction, address);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "(hl)");
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
        return;

        void OperateA()
        {
            ByteRegister.A.Operate(instruction, operation, change, count);
            ByteRegister.A.StoreToMemory(instruction, variable, offset);
        }
    }

    protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.PointerRegister pointerRegister,
        int offset, int count)
    {
        if (offset == 0) {
            var operand = "(" + pointerRegister + ")";
            if (Equals(pointerRegister, PointerRegister.Hl)) {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + operand);
                }
                return;
            }
            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadFromMemory(instruction, operand);
                ByteRegister.A.Operate(instruction, operation, change, count);
                ByteRegister.A.StoreToMemory(instruction, operand);
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
                instruction.WriteLine("\tld\t(" + pointerRegister + ")," + value);
                return;
            }
            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadConstant(instruction, value);
                instruction.WriteLine("\tld\t(" + pointerRegister + "),a");
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
        var candidates = new List<Cate.PointerRegister>(PointerRegister.Registers.Where(r => !Equals(r, pointerRegister)).ToList());
        using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
        reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
        StoreConstantIndirect(instruction, reservation.PointerRegister, offset, value);
    }

    public override void ClearByte(Instruction instruction, string label)
    {
        ByteOperation.ReserveRegister(instruction, ByteRegister.A);
        instruction.RemoveRegisterAssignment(ByteRegister.A);
        instruction.WriteLine("\txor\ta");
        instruction.WriteLine("\tld\t(" + label + "),a");
        instruction.AddChanged(ByteRegister.A);
    }

    public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
    {
        const string temporaryByte = Sm83.Compiler.TemporaryByte;
        register.StoreToMemory(instruction, temporaryByte);
        return temporaryByte;
    }
}