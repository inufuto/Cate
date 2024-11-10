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
        if (!instruction.IsRegisterReserved(WordRegister.Hl)) {
            using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
                WordRegister.Hl.LoadConstant(instruction, address);
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

    protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.WordRegister pointerRegister, int offset, int count)
    {
        if (Equals(pointerRegister, WordRegister.Hl)) {
            if (offset == 0) {
                var operand = "(" + pointerRegister + ")";
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + operand);
                }
                return;
            }
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                OperateIndirect(instruction, operation, change, pointerRegister, 0, count);
            });
            return;
        }
        using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
            WordRegister.Hl.CopyFrom(instruction, pointerRegister);
            OperateIndirect(instruction, operation, change, WordRegister.Hl, offset, count);
        }
    }

    protected override void OperateIndirect(Instruction instruction, string operation, bool change, Variable pointer, int offset, int count)
    {
        if (Equals(pointer.Register, WordRegister.Hl)) {
            OperateIndirect(instruction, operation, change, WordRegister.Hl, offset, count);
            return;
        }
        using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
            WordRegister.Hl.LoadFromMemory(instruction, pointer, 0);
            OperateIndirect(instruction, operation, change, WordRegister.Hl, offset, count);
        }
    }

    public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset, int value)
    {
        if (offset == 0) {
            if (Equals(pointerRegister, WordRegister.Hl)) {
                instruction.WriteLine("\tld\t(" + pointerRegister + ")," + value);
                return;
            }
            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadConstant(instruction, value);
                instruction.WriteLine("\tld\t(" + pointerRegister + "),a");
            }
            return;
        }
        if (Equals(pointerRegister, WordRegister.Hl)) {
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                StoreConstantIndirect(instruction, pointerRegister, 0, value);
            });
            return;
        }
        var candidates = new List<Cate.WordRegister>(WordRegister.Registers.Where(r => !Equals(r, pointerRegister)).ToList());
        using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
        reservation.WordRegister.CopyFrom(instruction, pointerRegister);
        StoreConstantIndirect(instruction, reservation.WordRegister, offset, value);
    }

    public override void ClearByte(Instruction instruction, string label)
    {
        ByteOperation.ReserveRegister(instruction, ByteRegister.A);
        instruction.RemoveRegisterAssignment(ByteRegister.A);
        instruction.WriteLine("\txor\ta,a");
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