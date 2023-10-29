using System;

namespace Inu.Cate.Mc6809;

internal class PointerAddOrSubtractInstruction : AddOrSubtractInstruction
{
    public PointerAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

    protected override int Threshold() => 0;

    public override void BuildAssembly()
    {
        if (LeftOperand.Type is not PointerType) {
            ExchangeOperands();
        }
        if (AddConstant())
            return;

        var operation = OperatorId switch
        {
            '+' => "add",
            '-' => "sub",
            _ => throw new NotImplementedException()
        };

        void AddD()
        {
            PointerRegister.D.Load(this, LeftOperand);
            PointerRegister.D.Operate(this, operation, true, RightOperand);
            PointerRegister.D.Store(this, DestinationOperand);
            ResultFlags |= Flag.Z;
        }
        if (Equals(LeftOperand.Register, PointerRegister.D)) {
            if (Equals(DestinationOperand.Register, PointerRegister.D)) {
                AddD();
                return;
            }
            if (!PointerRegister.D.Conflicts(RightOperand.Register)) {
                using (PointerOperation.ReserveRegister(this, PointerRegister.D)) {
                    AddD();
                    return;
                }
            }
        }

        if (Equals(RightOperand.Register, WordRegister.D)) {
            ViaIndex();
        }
        else {
            using (WordOperation.ReserveRegister(this, WordRegister.D)) {
                WordRegister.D.Load(this, RightOperand);
                ViaIndex();
            }
        }
        return;

        void ViaIndex()
        {
            if (LeftOperand.Register is PointerRegister leftPointerRegister) {
                ViaLeftIndex(leftPointerRegister);
            }
            else {
                using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.IndexRegisters, DestinationOperand);
                ViaLeftIndex(reservation.PointerRegister);
            }
        }


        void ViaLeftIndex(Cate.PointerRegister leftRegister)
        {
            leftRegister.Load(this, LeftOperand);
            WriteLine("\tlea" + leftRegister.AsmName + "\td," + leftRegister.AsmName);
            leftRegister.Store(this, DestinationOperand);
        }
    }

    private bool AddConstant()
    {
        if (RightOperand is IntegerOperand integerOperand) {
            if (Equals(LeftOperand.Register, PointerRegister.D) && Equals(DestinationOperand.Register, PointerRegister.D))
                return false;

            var value = integerOperand.IntegerValue;
            if (OperatorId == '-') {
                value = -value;
            }

            {
                return AddConstant(value.ToString());
            }
        }
        //if (RightOperand is ConstantOperand { Type: PointerType } constantOperand) {
        //    return AddConstant(constantOperand.MemoryAddress());
        //}
        return false;
    }

    private bool AddConstant(string value)
    {
        void ViaRegister(Cate.PointerRegister r)
        {
            r.Load(this, LeftOperand);
            if (Equals(r, PointerRegister.D)) {
                WriteLine("\taddd\t#" + value);
            }
            else {
                WriteLine("\tlea" + r + "\t" + value + "," + r);
            }
            r.Store(this, DestinationOperand);
            AddChanged(r);
            RemoveRegisterAssignment(r);
        }

        if (DestinationOperand.Register is PointerRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return true;
        }
        if (LeftOperand is VariableOperand variableOperand) {
            var variableRegister = GetVariableRegister(variableOperand);
            if (variableRegister is PointerRegister pointerRegister && !RightOperand.Conflicts(pointerRegister)) {
                ViaRegister(pointerRegister);
                return true;
            }
        }

        using var reservation = PointerOperation.ReserveAnyRegister(this, DestinationOperand);
        ViaRegister(reservation.PointerRegister);
        return true;
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