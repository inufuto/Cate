using System.Diagnostics;

namespace Inu.Cate.Sm83;

internal class WordBitInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : BinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (LeftOperand is ConstantOperand && !(RightOperand is ConstantOperand)) {
            ExchangeOperands();
        }

        var operation = OperatorId switch
        {
            '|' => "or\ta,",
            '^' => "xor\ta,",
            '&' => "and\ta,",
            _ => throw new ArgumentException(OperatorId.ToString())
        };
        {
            void ViaRegister(Cate.WordRegister r)
            {
                using var leftReservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, LeftOperand);
                var leftRegister = leftReservation.WordRegister;
                leftRegister.Load(this, LeftOperand);
                if (RightOperand is IntegerOperand integerOperand) {
                    var value = integerOperand.IntegerValue;
                    Operate(operation, r, leftRegister, "low " + value, "high " + value);
                    return;
                }

                using var rightReservation =
                    WordOperation.ReserveAnyRegister(this, WordRegister.Registers, RightOperand);
                var rightRegister = rightReservation.WordRegister;
                rightRegister.Load(this, RightOperand);
                Debug.Assert(rightRegister is { Low: not null, High: not null });
                Operate(operation, r, leftRegister, rightRegister.Low.Name, rightRegister.High.Name);
            }

            if (DestinationOperand.Register is WordRegister wordRegister && wordRegister != RightOperand.Register) {
                ViaRegister(wordRegister);
                return;
            }
            using var destinationReservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers);
            var destinationRegister = destinationReservation.WordRegister;
            ViaRegister(destinationRegister);
            destinationRegister.Store(this, DestinationOperand);
        }
    }

    private void Operate(string operation, Cate.WordRegister destinationRegister, Cate.WordRegister leftRegister,
        string rightLow, string rightHigh)
    {
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            Debug.Assert(leftRegister is { Low: not null, High: not null });
            Debug.Assert(destinationRegister is { Low: not null, High: not null });
            ByteRegister.A.CopyFrom(this, leftRegister.Low);
            WriteLine("\t" + operation + rightLow);
            destinationRegister.Low.CopyFrom(this, ByteRegister.A);
            ByteRegister.A.CopyFrom(this, leftRegister.High);
            WriteLine("\t" + operation + rightHigh);
            destinationRegister.High.CopyFrom(this, ByteRegister.A);
        }
    }
}