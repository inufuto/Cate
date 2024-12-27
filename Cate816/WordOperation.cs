namespace Inu.Cate.Wdc65816;

internal class WordOperation : Cate.WordOperation
{
    public override List<Cate.WordRegister> Registers => ((List<Cate.WordRegister>)[WordRegister.A, WordRegister.X, WordRegister.Y]).Union(WordZeroPage.Registers).ToList();

    public static void OperateBinomial(BinomialInstruction instruction, string operation)
    {
        switch (instruction.RightOperand) {
            case IntegerOperand integerOperand:
                OperateConstant(instruction, operation, integerOperand.IntegerValue.ToString());
                return;
            case StringOperand stringOperand:
                OperateConstant(instruction, operation, stringOperand.StringValue);
                return;
            case PointerOperand pointerOperand:
                OperateConstant(instruction, operation, pointerOperand.MemoryAddress());
                return;
            case VariableOperand when instruction.RightOperand.Register is WordRegister wordRegister:
                using (var reservation = WordOperation.ReserveAnyRegister(instruction, WordZeroPage.Registers)) {
                    var zeroPage = (WordZeroPage)reservation.WordRegister;
                    zeroPage.CopyFrom(instruction, wordRegister);
                    OperateMemory(instruction, operation, zeroPage.Name);
                }
                return;
            case VariableOperand when instruction.RightOperand.Register is WordZeroPage zeroPage:
                OperateMemory(instruction, operation, zeroPage.Name);
                return;
            case VariableOperand variableOperand:
                OperateMemory(instruction, operation, variableOperand.MemoryAddress());
                return;
            case IndirectOperand indirectOperand:
                var pointer = indirectOperand.Variable;
                var offset = indirectOperand.Offset;
                var variableRegister = instruction.GetVariableRegister(indirectOperand.Variable, 0);
                if (variableRegister is Cate.WordRegister pointerRegister) {
                    OperateIndirect(instruction, operation, pointerRegister, offset);
                    return;
                }
                OperateIndirect(instruction, operation, pointer, offset);
                return;
        }
        throw new NotImplementedException();
    }

    private static void OperateConstant(BinomialInstruction instruction, string operation, string value)
    {
        if (Equals(instruction.DestinationOperand.Register, WordRegister.A)) {
            ViaA();
        }
        else {
            using (WordOperation.ReserveRegister(instruction, WordRegister.A)) {
                ViaA();
            }
        }
        return;

        void ViaA()
        {
            WordRegister.A.Load(instruction, instruction.LeftOperand);
            ModeFlag.Memory.ResetBit(instruction);
            instruction.WriteLine("\t" + operation + "\t#" + value);
            instruction.AddChanged(WordRegister.A);
            WordRegister.A.Store(instruction, instruction.DestinationOperand);
        }
    }

    private static void OperateMemory(BinomialInstruction instruction, string operation, string label)
    {
        if (Equals(instruction.DestinationOperand.Register, WordRegister.A)) {
            ViaA();
        }
        else {
            using (WordOperation.ReserveRegister(instruction, WordRegister.A)) {
                ViaA();
            }
        }
        return;

        void ViaA()
        {
            WordRegister.A.Load(instruction, instruction.LeftOperand);
            ModeFlag.Memory.ResetBit(instruction);
            instruction.WriteLine("\t" + operation + "\t" + label);
            WordRegister.A.Store(instruction, instruction.DestinationOperand);
        }
    }

    private static void OperateIndirect(BinomialInstruction instruction, string operation, Cate.WordRegister pointerRegister, int offset)
    {
        switch (pointerRegister) {
            case WordIndexRegister wordIndexRegister: {
                    WordRegister.A.Load(instruction, instruction.LeftOperand);
                    wordIndexRegister.MakeSize(instruction);
                    ModeFlag.Memory.ResetBit(instruction);
                    instruction.WriteLine("\t" + operation + "\t>" + offset + "," + wordIndexRegister);
                    break;
                }
            case WordZeroPage wordZeroPage:
                switch (offset) {
                    case 0: {
                            WordRegister.A.Load(instruction, instruction.LeftOperand);
                            ModeFlag.Memory.ResetBit(instruction);
                            instruction.WriteLine("\t" + operation + "\t(" + wordZeroPage + ")");
                            break;
                        }
                    //case >= 0 and < 0x100: {
                    //        using (ByteOperation.ReserveRegister(instruction, ByteRegister.Y)) {
                    //            ByteRegister.Y.LoadConstant(instruction, offset);
                    //            WordRegister.A.Load(instruction, instruction.LeftOperand);
                    //            ModeFlag.Memory.ResetBit(instruction);
                    //            instruction.WriteLine("\t" + operation + "\t(" + wordZeroPage + "),y");
                    //        }
                    //        break;
                    //    }
                    default: {
                            using (WordOperation.ReserveRegister(instruction, WordRegister.Y)) {
                                WordRegister.Y.LoadConstant(instruction, offset);
                                WordRegister.A.Load(instruction, instruction.LeftOperand);
                                ModeFlag.Memory.ResetBit(instruction);
                                instruction.WriteLine("\t" + operation + "\t(" + wordZeroPage + "),y");
                            }
                            break;
                        }
                }
                break;
            default:
                throw new NotImplementedException();
        }

    }

    private static void OperateIndirect(BinomialInstruction instruction, string operation, Variable pointer, int offset)
    {
        using var reservation = Compiler.WordOperation.ReserveAnyRegister(instruction, [WordRegister.X, WordRegister.Y,]);
        reservation.WordRegister.LoadFromMemory(instruction, pointer, 0);
        OperateIndirect(instruction, operation, reservation.WordRegister, offset);
    }

    public override List<Cate.WordRegister> PointerRegisters => WordRegister.PointerRegisters;

    public override List<Cate.WordRegister> RegistersToOffset(int offset)
    {
        return PointerRegisters;
    }

    public override void ClearWord(Instruction instruction, string label)
    {
        ModeFlag.Memory.ResetBit(instruction);
        instruction.WriteLine("\tstz\t" + label);
    }
}