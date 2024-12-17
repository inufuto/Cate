namespace Inu.Cate.Wdc65816;

internal class WordAddOrSubtractInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : Cate.AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (Equals(RightOperand.Register, WordRegister.A) && !Equals(LeftOperand.Register, WordRegister.A) && IsOperatorExchangeable()) {
            ExchangeOperands();
        }
        if (CanIncrementOrDecrement() && IncrementOrDecrement())
            return;

        var operation = OperatorId switch
        {
            '+' => "clc|adc",
            '-' => "sec|sbc",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Flag.Z;

        Wdc65816.WordOperation.OperateBinomial(this, operation);
    }

    private bool CanIncrementOrDecrement()
    {
        if (LeftOperand is IndirectOperand)
            return false;
        if (DestinationOperand is IndirectOperand)
            return false;
        if (Equals(LeftOperand.Register, WordRegister.A))
            return false;
        if (Equals(DestinationOperand.Register, WordRegister.A))
            return false;
        return true;
    }

    protected override int Threshold() => 4;

    protected override void Increment(int count)
    {
        IncrementOrDecrement("in", "inc", count);
        ResultFlags |= Flag.Z;
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("de", "dec", count);
        ResultFlags |= Flag.Z;
    }

    private void IncrementOrDecrement(string registerOperation, string memoryOperation, int count)
    {
        if (DestinationOperand.SameStorage(LeftOperand) && count == 1) {
            if (DestinationOperand.Register is WordRegister wordRegister) {
                wordRegister.MakeSize(this);
                if (wordRegister.Equals(WordRegister.A)) {
                    WriteLine("\t" + memoryOperation + "\t" + wordRegister);
                }
                else {
                    WriteLine("\t" + registerOperation + wordRegister);
                }
                return;
            }
            if (DestinationOperand.Register is WordZeroPage wordZeroPage)
            {
                ModeFlag.Memory.ResetBit(this);
                WriteLine("\t" + memoryOperation + "\t" + wordZeroPage);
                return;
            }
            if (DestinationOperand is VariableOperand variableOperand) {
                ModeFlag.Memory.ResetBit(this);
                WriteLine("\t" + memoryOperation + "\t" + variableOperand.MemoryAddress());
                return;
            }
        }
        {
            if (DestinationOperand.Register is WordRegister wordRegister && !RightOperand.Conflicts(wordRegister)) {
                ViaRegister(wordRegister);
                return;
            }
        }
        var candidates = new List<Cate.WordRegister> { WordRegister.X, WordRegister.Y };
        using var reservation = WordOperation.ReserveAnyRegister(this, candidates, LeftOperand);
        ViaRegister((WordRegister)reservation.WordRegister);
        reservation.WordRegister.Store(this, DestinationOperand);
        return;

        void ViaRegister(WordRegister r)
        {
            r.MakeSize(this);
            r.Load(this, LeftOperand);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + registerOperation + r);
            }
            RemoveRegisterAssignment(r);
            AddChanged(r);
        }
    }
}