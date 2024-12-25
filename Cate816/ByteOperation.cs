namespace Inu.Cate.Wdc65816;

internal class ByteOperation : Cate.ByteOperation
{
    public override List<Cate.ByteRegister> Accumulators => [ByteRegister.A];
    protected override void OperateConstant(Instruction instruction, string operation, string value, int count)
    {
        ModeFlag.Memory.SetBit(instruction);
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + "\t#" + value);
        }
    }

    protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset, int count)
    {
        ModeFlag.Memory.SetBit(instruction);
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + "\t" + variable.MemoryAddress(offset));
        }
        instruction.RemoveVariableRegister(variable, offset);
        //instruction.ResultFlags |= Instruction.Flag.Z;
    }

    protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.WordRegister pointerRegister, int offset,
        int count)
    {
        switch (pointerRegister) {
            case WordIndexRegister wordIndexRegister: {
                    wordIndexRegister.MakeSize(instruction);
                    ModeFlag.Memory.SetBit(instruction);
                    for (var i = 0; i < count; ++i) {
                        instruction.WriteLine("\t" + operation + "\t>" + offset + "," + wordIndexRegister);
                    }
                    break;
                }
            case WordZeroPage wordZeroPage:
                switch (offset) {
                    case 0: {
                            ModeFlag.Memory.SetBit(instruction);
                            for (var i = 0; i < count; ++i) {
                                instruction.WriteLine("\t" + operation + "\t(" + wordZeroPage + ")");
                            }

                            break;
                        }
                    //case >= 0 and < 0x100: {
                    //        using (ByteOperation.ReserveRegister(instruction, ByteRegister.Y)) {
                    //            ByteRegister.Y.LoadConstant(instruction, offset);
                    //            for (var i = 0; i < count; ++i) {
                    //                instruction.WriteLine("\t" + operation + "\t(" + wordZeroPage + "),y");
                    //            }
                    //        }

                    //        break;
                    //    }
                    default: {
                            using (WordOperation.ReserveRegister(instruction, WordRegister.Y)) {
                                WordRegister.Y.LoadConstant(instruction, offset);
                                for (var i = 0; i < count; ++i) {
                                    instruction.WriteLine("\t" + operation + "\t(" + wordZeroPage + "),y");
                                }
                            }

                            break;
                        }
                }
                break;
            default: {
                    using var reservation = WordOperation.ReserveAnyRegister(instruction, [WordRegister.X, WordRegister.Y]);
                    reservation.WordRegister.CopyFrom(instruction, pointerRegister);
                    OperateIndirect(instruction, operation, change, reservation.WordRegister, offset, count);
                    break;
                }
        }
    }

    public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset, int value)
    {
        if (Equals(pointerRegister, WordRegister.A)) {
            using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.PointerRegisters);
            reservation.WordRegister.CopyFrom(instruction, pointerRegister);
            StoreConstantIndirect(instruction, reservation.WordRegister, offset, value);
            return;
        }
        base.StoreConstantIndirect(instruction, pointerRegister, offset, value);
    }

    public override List<Cate.ByteRegister> Registers => ByteRegister.Registers.Union(ByteZeroPage.Registers).ToList();

    public override void ClearByte(Instruction instruction, string label)
    {
        ModeFlag.Memory.SetBit(instruction);
        instruction.WriteLine("\tstz\t" + label);
    }

    public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
    {
        var label = Wdc65816.Compiler.TemporaryWordLabel;
        register.StoreToMemory(instruction, "<" + label);
        return label;
    }

}