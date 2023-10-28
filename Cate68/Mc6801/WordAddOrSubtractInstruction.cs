using System;

namespace Inu.Cate.Mc6800.Mc6801;

internal class WordAddOrSubtractInstruction : Cate.AddOrSubtractInstruction
{
    public WordAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
    {
    }

    public override void BuildAssembly()
    {
        if (IncrementOrDecrement())
            return;

        var operation = OperatorId switch
        {
            '+' => "add",
            '-' => "sub",
            _ => throw new NotImplementedException()
        };

        using (WordOperation.ReserveRegister(this, PairRegister.D, DestinationOperand)) {
            PairRegister.D.Load(this, LeftOperand);
            PairRegister.D.Operate(this, operation, false, RightOperand);
            PairRegister.D.Store(this, DestinationOperand);
        }
    }

    protected override int Threshold() => 2;

    protected override void Increment(int count)
    {
        IncrementOrDecrement("inx", count);
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("dex", count);
    }

    private void IncrementOrDecrement(string operation, int count)
    {
        using (WordOperation.ReserveRegister(this, IndexRegister.X)) {
            IndexRegister.X.Load(this, LeftOperand);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + operation);
            }
            IndexRegister.X.Store(this, DestinationOperand);
        }
    }
}