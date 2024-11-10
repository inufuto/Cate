using System;

namespace Inu.Cate.Tms99;

internal class WordAddOrSubtractInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (IsOperatorExchangeable()) {
            if (RightOperand.Register != null && LeftOperand.Register == null) {
                ExchangeOperands();
            }
            //else if (LeftOperand.Type is not PointerType) {
            //    ExchangeOperands();
            //}
        }
        if (IncrementOrDecrement()) return;

        ResultFlags |= Flag.Z;
        if (RightOperand is IntegerOperand integerOperand) {
            var value = OperatorId switch
            {
                '+' => integerOperand.IntegerValue,
                '-' => -integerOperand.IntegerValue,
                _ => throw new NotImplementedException()
            };
            Tms99.WordOperation.OperateConstant(this, "ai", DestinationOperand, LeftOperand, value);
            return;
        }
        if (RightOperand is PointerOperand pointerOperand) {
            var value = OperatorId switch
            {
                '+' => pointerOperand.MemoryAddress(),
                '-' => "-" + pointerOperand.MemoryAddress(),
                _ => throw new NotImplementedException()
            };
            Tms99.WordOperation.OperateConstant(this, "ai", DestinationOperand, LeftOperand, value);
            return;
        }

        var operation = OperatorId switch
        {
            '+' => "a",
            '-' => "s",
            _ => throw new NotImplementedException()
        };
        Tms99.WordOperation.Operate(this, operation, DestinationOperand, LeftOperand, RightOperand);
    }



    protected override int Threshold() => 4;

    private void IncrementOrDecrement(string operation, int count)
    {
        if (LeftOperand.SameStorage(DestinationOperand)) {
            for (var i = 0; i < count; ++i) {
                Tms99.WordOperation.Operate(this, operation, DestinationOperand);
            }
            return;
        }

        void ForRegister(Cate.WordRegister wordRegister)
        {
            wordRegister.Load(this, LeftOperand);
            while (count > 2) {
                var half = count / 2;
                for (var i = 0; i < half; ++i) {
                    WriteLine("\t" + operation + "t\t" + wordRegister.Name);
                }
                count -= half * 2;
            }
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + operation + "\t" + wordRegister.Name);
            }
            if (!Equals(DestinationOperand.Register, wordRegister)) {
                AddChanged(wordRegister);
            }
            wordRegister.Store(this, DestinationOperand);
        }
        if (DestinationOperand.Register is WordRegister destinationRegister) {
            ForRegister(destinationRegister);
        }
        else if (LeftOperand.Register is WordRegister leftRegister) {
            ForRegister(leftRegister);
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(this);
            ForRegister(reservation.WordRegister);
        }
        ResultFlags |= Flag.Z;
    }

    protected override void Increment(int count)
    {
        IncrementOrDecrement("inc", count);
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("dec", count);
    }
}