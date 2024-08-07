using System;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Z80;

internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
{
    public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand leftOperand, Operand rightOperand)
        : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
    {
        Debug.Assert(destinationOperand.Type.ByteCount == 1);
    }

    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (
            Equals(register, ByteRegister.A) &&
            !IsOperatorExchangeable() &&
            RightOperand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)
        )
            return null;
        return base.RegisterAdaptability(variable, register);
    }

    public override void BuildAssembly()
    {
        if (
            Equals(RightOperand.Register, ByteRegister.A) &&
            !Equals(LeftOperand.Register, ByteRegister.A) &&
            IsOperatorExchangeable()
        ) {
            ExchangeOperands();
        }

        if (IncrementOrDecrement())
            return;

        var operation = OperatorId switch
        {
            '+' => "add\ta,",
            '-' => "sub\t",
            _ => throw new NotImplementedException()
        };
        ResultFlags |= Flag.Z;

        if (Equals(RightOperand.Register, ByteRegister.A)) {
            var candidates = ByteRegister.Registers.Where(r => !Equals(r, ByteRegister.A)).ToList();
            using var reservation = ByteOperation.ReserveAnyRegister(this, candidates);
            var byteRegister = reservation.ByteRegister;
            byteRegister.CopyFrom(this, ByteRegister.A);
            ByteRegister.A.Load(this, LeftOperand);
            WriteLine("\t" + operation + byteRegister);
            ByteRegister.A.Store(this, DestinationOperand);
            AddChanged(ByteRegister.A);
            return;
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
            ByteRegister.A.Load(this, LeftOperand);
            ByteRegister.A.Operate(this, operation, true, RightOperand);
            ByteRegister.A.Store(this, DestinationOperand);
            AddChanged(ByteRegister.A);
        }
    }



    protected override int Threshold()
    {
        return LeftOperand.Register == null || Equals(LeftOperand.Register, ByteRegister.A) ? 1 : 4;
    }

    protected override void Increment(int count)
    {
        OperateByte("inc\t", count);
        ResultFlags |= Flag.Z;
    }

    protected override void Decrement(int count)
    {
        OperateByte("dec\t", count);
        ResultFlags |= Flag.Z;
    }
}