﻿using Inu.Language;

namespace Inu.Cate
{
    public abstract class BinomialInstruction : Instruction
    {
        public readonly int OperatorId;
        public readonly AssignableOperand DestinationOperand;
        public Operand LeftOperand { get; private set; }
        public Operand RightOperand { get; private set; }

        public override Operand? ResultOperand => DestinationOperand;


        protected BinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand) : base(function)
        {
            OperatorId = operatorId;
            DestinationOperand = destinationOperand;
            LeftOperand = leftOperand;
            RightOperand = rightOperand;

            DestinationOperand.AddUsage(function.NextAddress, Variable.Usage.Write);
            LeftOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
            RightOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
        }

        public override void ReserveOperandRegisters()
        {
            ReserveOperandRegister(LeftOperand);
            ReserveOperandRegister(RightOperand);
            if (DestinationOperand is IndirectOperand indirectOperand) {
                ReserveOperandRegister(indirectOperand);
            }
        }

        public override bool IsSourceOperand(Variable variable)
        {
            return LeftOperand.IsVariable(variable) || RightOperand.IsVariable(variable);
        }

        //public override void RemoveDestinationRegister()
        //{
        //    RemoveChangedRegisters(DestinationOperand);
        //}

        public override string ToString() => DestinationOperand + " = " + LeftOperand + " " + ReservedWord.FromId(OperatorId) + " " + RightOperand;

        protected bool IsOperatorExchangeable()
        {
            switch (OperatorId) {
                case '+':
                case '|':
                case '^':
                case '&':
                    return true;
            }
            return false;
        }

        protected void ExchangeOperands()
        {
            (LeftOperand, RightOperand) = (RightOperand, LeftOperand);
        }

        protected virtual void OperateByte(string operation, int count)
        {
            if (DestinationOperand.Equals(LeftOperand) && count == 1 && IsOperatable(DestinationOperand)) {
                ByteOperation.Operate(this, operation, true, DestinationOperand, count);
                return;
            }

            void ViaRegister(ByteRegister register)
            {
                register.Load(this, LeftOperand);
                if (count != 0) {
                    register.Operate(this, operation, true, count);
                }
                RemoveRegisterAssignment(register);
            }

            if (DestinationOperand.Register is ByteRegister byteRegister && !Equals(RightOperand.Register, byteRegister)) {
                ViaRegister(byteRegister);
                return;
            }
            using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
            ViaRegister(reservation.ByteRegister);
            reservation.ByteRegister.Store(this, DestinationOperand);
        }

        protected virtual bool IsOperatable(AssignableOperand operand) => true;
    }
}