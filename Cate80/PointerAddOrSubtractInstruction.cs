using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Z80;

internal class PointerAddOrSubtractInstruction : AddOrSubtractInstruction
{
    private static readonly List<Cate.WordRegister> RightCandidates = new()
        {WordRegister.De, WordRegister.Bc};

    public PointerAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

    protected override int Threshold() => 4;

    private WordRegister? RightWordRegister()
    {
        if (RightOperand is not VariableOperand variableOperand) return null;
        var variableRegister = GetVariableRegister(variableOperand);
        return variableRegister switch
        {
            WordRegister wordRegister => wordRegister,
            PointerRegister { WordRegister: WordRegister pointerWordRegister } => pointerWordRegister,
            _ => null
        };
    }


    public override void BuildAssembly()
    {
        if (LeftOperand.Type is not PointerType) {
            ExchangeOperands();
        }
        if (IncrementOrDecrement())
            return;

        if (OperatorId == '+') {
            if (RightWordRegister() is WordRegister { Addable: true } rightRegister && (LeftOperand.Register is not PointerRegister leftRegister || !leftRegister.IsAddable())) {
                using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers.Where(r => !r.IsAddable()).ToList());
                leftRegister = (PointerRegister)reservation.PointerRegister;
                leftRegister.Load(this, LeftOperand);
                WriteLine("\tadd\t" + rightRegister.Name + "," + leftRegister.Name);
                AddChanged(rightRegister);
                RemoveRegisterAssignment(rightRegister);
                rightRegister.Store(this, DestinationOperand);
                return;
            }
        }

        if (RightOperand is IntegerOperand integerOperand) {
            var value = OperatorId == '+' ? integerOperand.IntegerValue : -integerOperand.IntegerValue;
            AddConstant(value);
            return;
        }

        Action<Cate.PointerRegister> action;
        List<Cate.PointerRegister> candidates;
        switch (OperatorId) {
            case '+':
                action = AddRegister;
                candidates = new List<Cate.PointerRegister>() { PointerRegister.Hl, PointerRegister.Ix, PointerRegister.Iy };
                break;
            case '-':
                action = SubtractRegister;
                candidates = new List<Cate.PointerRegister>() { PointerRegister.Hl };
                break;
            default:
                throw new NotImplementedException();
        }
        if (DestinationOperand.Register is PointerRegister destinationRegister && candidates.Contains(destinationRegister)) {
            action(destinationRegister);
            return;
        }
        {
            using var reservation = PointerOperation.ReserveAnyRegister(this, candidates, LeftOperand);
            action(reservation.PointerRegister);
        }
    }


    private void AddConstant(int value)
    {
        void ViaRegister(Cate.PointerRegister r)
        {
            r.Load(this, LeftOperand);
            r.Add(this, value);
            r.Store(this, DestinationOperand);
        }

        if (DestinationOperand.Register is PointerRegister register) {
            if (register.IsAddable()) {
                ViaRegister(register);
                return;
            }
        }
        using var reservation = PointerOperation.ReserveAnyRegister(this, new List<Cate.PointerRegister> { PointerRegister.Hl, PointerRegister.Ix, PointerRegister.Iy }, LeftOperand);
        ViaRegister(reservation.PointerRegister);
    }

    private void AddRegister(Cate.PointerRegister register)
    {
        void ViaRegister(Cate.WordRegister r)
        {
            r.Load(this, RightOperand);
            register.Load(this, LeftOperand);
            WriteLine("\tadd\t" + register + "," + r);
            RemoveRegisterAssignment(register);
            AddChanged(register);
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, RightCandidates, RightOperand);
        ViaRegister(reservation.WordRegister);
        register.Store(this, DestinationOperand);
    }


    private void SubtractRegister(Cate.PointerRegister register)
    {
        void ViaRegister(Cate.WordRegister r)
        {
            r.Load(this, RightOperand);
            //CancelOperandRegister(RightOperand);
            register.Load(this, LeftOperand);
            WriteLine("\tor\ta");
            WriteLine("\tsbc\t" + register + "," + r);
            RemoveRegisterAssignment(register);
            RemoveRegisterAssignment(register);
        }
        Debug.Assert(Equals(register, PointerRegister.Hl));
        using var reservation = WordOperation.ReserveAnyRegister(this, RightCandidates, RightOperand);
        ViaRegister(reservation.WordRegister);
        register.Store(this, DestinationOperand);
    }

    protected override void Increment(int count)
    {
        IncrementOrDecrement("inc", count);
    }

    protected override void Decrement(int count)
    {
        IncrementOrDecrement("dec", count);
    }

    private void IncrementOrDecrement(string operation, int count)
    {
        void ViaRegister(Register register)
        {
            Debug.Assert(count >= 0);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + operation + "\t" + register);
            }
            RemoveRegisterAssignment(register);
            AddChanged(register);
        }

        if (DestinationOperand.Register is Cate.PointerRegister destinationRegister) {
            destinationRegister.Load(this, LeftOperand);
            ViaRegister(destinationRegister);
            return;
        }
        using var reservation = PointerOperation.ReserveAnyRegister(this, PointerRegister.Registers.Where(r => r.IsAddable()).ToList(), LeftOperand);
        reservation.PointerRegister.Load(this, LeftOperand);
        ViaRegister(reservation.PointerRegister);
        reservation.PointerRegister.Store(this, DestinationOperand);
        ;
    }
}