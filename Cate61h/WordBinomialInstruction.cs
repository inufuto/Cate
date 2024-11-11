namespace Inu.Cate.Hd61700;

internal class WordBinomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : Cate.BinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    public override void BuildAssembly()
    {
        if (
            !Equals(RightOperand.Register, null) &&
            Equals(LeftOperand.Register, null) &&
            IsOperatorExchangeable()
        ) {
            ExchangeOperands();
        }

        var operation = OperatorId switch
        {
            '+' => "adw",
            '-' => "sbw",
            '|' => "orw",
            '^' => "xrw",
            '&' => "anw",
            _ => throw new NotImplementedException()
        };

        if (DestinationOperand.Register is WordRegister destinationRegister && !Equals(RightOperand.Register, destinationRegister)) {
            ViaRegister(destinationRegister);
            return;
        }

        if (LeftOperand is VariableOperand variableOperand) {
            var register = GetVariableRegister(variableOperand);
            if (register is WordRegister wordRegister) {
                ViaRegister(wordRegister);
                return;
            }
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers);
        {
            var register = reservation.WordRegister;
            ViaRegister(register);
        }
        return;

        void ViaRegister(Cate.WordRegister register)
        {
            register.Load(this, LeftOperand);
            register.Operate(this, operation, true, RightOperand);
            register.Store(this, DestinationOperand);
        }
    }
}