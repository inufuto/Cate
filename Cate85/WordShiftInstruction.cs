using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class WordShiftInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : Cate.WordShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override void ShiftConstant(int count)
    {
        if (count == 8) {
            if (OperatorId == Keyword.ShiftRight && !((IntegerType)LeftOperand.Type).Signed) {
                using var reservation = WordOperation.ReserveAnyRegister(this, DestinationOperand);
                var wordRegister = reservation.WordRegister;
                wordRegister.Load(this, LeftOperand);
                Debug.Assert(wordRegister is { Low: not null, High: not null });
                wordRegister.Low.CopyFrom(this, wordRegister.High);
                wordRegister.High.LoadConstant(this, 0);
                return;
            }
            if (OperatorId == Keyword.ShiftLeft) {
                using var reservation = WordOperation.ReserveAnyRegister(this, DestinationOperand);
                var wordRegister = reservation.WordRegister;
                wordRegister.Load(this, LeftOperand);
                Debug.Assert(wordRegister is { Low: not null, High: not null });
                wordRegister.High.CopyFrom(this, wordRegister.Low);
                wordRegister.Low.LoadConstant(this, 0);
                return;
            }
        }
        {

            Action<Cate.WordRegister> action = OperatorId switch
            {
                Keyword.ShiftLeft => register =>
                {
                    WriteLine("\tsll\t" + register.Low);
                    WriteLine("\trlc\t" + register.High);
                }
                ,
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => register =>
                {
                    WriteLine("\tsra\t" + register.High);
                    WriteLine("\trrc\t" + register.Low);
                }
                ,
                Keyword.ShiftRight => register =>
                {
                    WriteLine("\tsrl\t" + register.High);
                    WriteLine("\trrc\t" + register.Low);
                }
                ,
                _ => throw new NotImplementedException()
            };
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
                for (var i = 0; i < count; ++i) {
                    action(register);
                }

                register.Store(this, DestinationOperand);
            }
        }
    }

    protected override void ShiftVariable(Operand counterOperand)
    {
        var functionName = OperatorId switch
        {
            Keyword.ShiftLeft => "cate.ShiftLeftWord",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                ? "cate.ShiftRightSignedWord"
                : "cate.ShiftRightWord",
            _ => throw new NotImplementedException()
        };
        var valueRegister = WordRegister.FromAddress(0);
        var counterRegister = ByteRegister.FromAddress(3);
        using (WordOperation.ReserveRegister(this, valueRegister, LeftOperand)) {
            valueRegister.Load(this, LeftOperand);
            using (ByteOperation.ReserveRegister(this, counterRegister)) {
                counterRegister.Load(this, counterOperand);
                Compiler.CallExternal(this, functionName);
            }
            AddChanged(valueRegister);
            RemoveRegisterAssignment(valueRegister);
            valueRegister.Store(this, DestinationOperand);
        }
    }

    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (RightOperand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)) {
            if (register.Conflicts(ByteRegister.FromAddress(3))) {
                return 1;
            }
        }
        return base.RegisterAdaptability(variable, register);
    }
}