using System;

namespace Inu.Cate.Mc6809;

internal class WordAddOrSubtractInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : AddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override int Threshold() => 0;

    public override void BuildAssembly()
    {
        if (IsOperatorExchangeable()) {
            if (LeftOperand is ConstantOperand && RightOperand is not ConstantOperand) {
                ExchangeOperands();
            }
            else if (LeftOperand.Register == null && RightOperand.Register != null) {
                ExchangeOperands();
            }
            //else if (LeftOperand.Type is not PointerType && RightOperand.Type is PointerType) {
            //    ExchangeOperands();
            //}
        }

        if (AddConstant())
            return;

        if (DestinationOperand.Register is WordRegister wordRegister) {
            ViaRegister(wordRegister);
            return;
        }

        using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
        ViaRegister(reservation.WordRegister);
        return;

        void ViaRegister(Cate.WordRegister register)
        {
            register.Load(this, LeftOperand);
            if (register.Equals(WordRegister.D)) {
                var operation = OperatorId switch
                {
                    '+' => "add",
                    '-' => "sub",
                    _ => throw new NotImplementedException()
                };
                register.Operate(this, operation, true, RightOperand);
            }
            else {
                if (Equals(RightOperand.Register, WordRegister.D)) {
                    AddXd(register);
                }
                else {
                    using (WordOperation.ReserveRegister(this, WordRegister.D)) {
                        using (WordOperation.ReserveRegister(this, WordRegister.X)) {
                            WordRegister.D.Load(this, RightOperand);
                        }
                        AddXd(register);
                    }
                }
            }
            register.Store(this, DestinationOperand);
            ResultFlags |= Flag.Z;
        }
        void AddXd(Cate.WordRegister register)
        {
            WriteLine("\tlea" + register.AsmName + "\td," + register.AsmName);
        }
    }

    private bool AddConstant()
    {
        if (RightOperand is IntegerOperand integerOperand) {
            if (Equals(LeftOperand.Register, WordRegister.D) && Equals(DestinationOperand.Register, WordRegister.D))
                return false;

            var value = integerOperand.IntegerValue;
            if (OperatorId == '-') {
                value = -value;
            }
            {
                return AddConstant(value.ToString());
            }
        }
        return false;
    }

    private bool AddConstant(string value)
    {
        switch (DestinationOperand.Register) {
            case WordRegister wordRegister when RightOperand.Conflicts(wordRegister):
                ViaRegister(wordRegister);
                return true;
        }
        if (DestinationOperand.Register is WordRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return true;
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
        ViaRegister(reservation.WordRegister);
        reservation.WordRegister.Store(this, DestinationOperand);

        return true;

        void ViaRegister(Cate.WordRegister register)
        {
            register.Load(this, LeftOperand);
            if (Equals(register, WordRegister.D)) {
                WriteLine("\taddd\t#" + value);
            }
            else {
                WriteLine("\tlea" + register + "\t" + value + "," + register);
            }

            AddChanged(register);
            RemoveRegisterAssignment(register);
        }
    }


    protected override void Increment(int count)
    {
        throw new NotImplementedException();
    }

    protected override void Decrement(int count)
    {
        throw new NotImplementedException();
    }
}