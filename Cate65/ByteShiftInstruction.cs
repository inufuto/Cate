using System;

namespace Inu.Cate.Mos6502;

internal class ByteShiftInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : Cate.ByteShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override string Operation()
    {
        return OperatorId switch
        {
            Keyword.ShiftLeft => "asl",
            Keyword.ShiftRight when !((IntegerType)LeftOperand.Type).Signed => "lsr",
            _ => throw new NotImplementedException()
        };
    }


    protected override void ShiftConstant(int count)
    {
        if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
            using (ByteOperation.ReserveRegister(this, ByteRegister.Y)) {
                ByteRegister.Y.LoadConstant(this, count);
                CallExternal("cate.ShiftRightSignedA");
            }
            return;
        }
        base.ShiftConstant(count);
    }

    protected override void ShiftVariable(Operand counterOperand)
    {
        var functionName = OperatorId switch
        {
            Keyword.ShiftLeft => "cate.ShiftLeftA",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                ? "cate.ShiftRightSignedA"
                : "cate.ShiftRightA",
            _ => throw new NotImplementedException()
        };
        using (ByteOperation.ReserveRegister(this, ByteRegister.Y)) {
            ByteRegister.Y.Load(this, RightOperand);
            CallExternal(functionName);
        }
    }

    private void CallExternal(string functionName)
    {
        if (Equals(DestinationOperand.Register, ByteRegister.A)) {
            Call();
            return;
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            Call();
        }

        return;

        void Call()
        {
            ByteRegister.A.Load(this, LeftOperand);
            Compiler.CallExternal(this, functionName);
            RemoveRegisterAssignment(ByteRegister.A);
            AddChanged(ByteRegister.A);
            RemoveRegisterAssignment(ByteRegister.Y);
            AddChanged(ByteRegister.Y);
            ByteRegister.A.Store(this, DestinationOperand);
        }
    }

    protected override void OperateByte(string operation, int count)
    {
        if (DestinationOperand.Equals(LeftOperand) && count == 1) {
            base.OperateByte(operation, count);
            return;
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
            ByteRegister.A.Load(this, LeftOperand);
            if (count != 0) {
                ByteRegister.A.Operate(this, operation, true, count);
            }
            RemoveRegisterAssignment(ByteRegister.A);
            ByteRegister.A.Store(this, DestinationOperand);
        }
    }
}