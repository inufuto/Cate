using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate.Z80
{
    internal class WordAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        private static readonly List<Cate.WordRegister> RightCandidates = new List<Cate.WordRegister>()
            {WordRegister.De, WordRegister.Bc};

        protected override int Threshold() => 4;

        public WordAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand)
            : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override bool CanAllocateRegister(Variable variable, Register register1)
        {
            if (RightOperand is VariableOperand rightVariableOperand && rightVariableOperand.Variable.Equals(variable)) {
                if (rightVariableOperand.Register is WordRegister wordRegister) {
                    if (!wordRegister.IsAddable())
                        return false;
                }
            }
            if (LeftOperand is VariableOperand leftVariableOperand && leftVariableOperand.Variable.Equals(variable)) {
                if (leftVariableOperand.Register is WordRegister wordRegister) {
                    if (!wordRegister.IsAddable())
                        return false;
                }
            }
            return base.CanAllocateRegister(variable, register1);
        }

        public override void BuildAssembly()
        {
            if (LeftOperand is ConstantOperand && !(RightOperand is ConstantOperand) && IsOperatorExchangeable()) {
                ExchangeOperands();
            }
            else {
                if (RightOperand.Register is WordRegister rightRegister) {
                    if (rightRegister.IsAddable() && IsOperatorExchangeable()) {
                        ExchangeOperands();
                    }
                }
            }

            if (IncrementOrDecrement())
                return;

            if (RightOperand is IntegerOperand integerOperand) {
                var value = OperatorId == '+' ? integerOperand.IntegerValue : -integerOperand.IntegerValue;
                AddConstant(value);
                return;
            }

            Action<Cate.WordRegister> action;
            List<Cate.WordRegister> candidates;
            switch (OperatorId) {
                case '+':
                    action = AddRegister;
                    candidates = WordRegister.AddableRegisters;
                    break;
                case '-':
                    action = SubtractRegister;
                    candidates = new List<Cate.WordRegister>() { WordRegister.Hl };
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (DestinationOperand.Register is WordRegister destinationRegister && candidates.Contains(destinationRegister)) {
                action(destinationRegister);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, candidates, LeftOperand);
            action(reservation.WordRegister);
        }

        private void AddConstant(int value)
        {
            void ViaRegister(Cate.WordRegister r)
            {
                r.Load(this, LeftOperand);
                r.Add(this, value);
                r.Store(this, DestinationOperand);
            }

            if (DestinationOperand.Register is WordRegister register) {
                if (register.IsAddable()) {
                    ViaRegister(register);
                    return;
                }
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.AddableRegisters, LeftOperand);
            ViaRegister(reservation.WordRegister);
        }

        private void AddRegister(Cate.WordRegister register)
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


        private void SubtractRegister(Cate.WordRegister register)
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
            Debug.Assert(Equals(register, WordRegister.Hl));
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
            if (DestinationOperand.Register is WordRegister destinationRegister) {
                destinationRegister.Load(this, LeftOperand);
                IncrementOrDecrement(this, operation, destinationRegister, count);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.AddableRegisters, LeftOperand);
            reservation.WordRegister.Load(this, LeftOperand);
            IncrementOrDecrement(this, operation, reservation.WordRegister, count);
            reservation.WordRegister.Store(this, DestinationOperand);
            ;
        }

        private static void IncrementOrDecrement(Instruction instruction, string operation, Cate.WordRegister leftRegister, int count)
        {
            Debug.Assert(count >= 0);
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + leftRegister);
            }
            instruction.RemoveRegisterAssignment(leftRegister);
            instruction.AddChanged(leftRegister);
        }
    }
}