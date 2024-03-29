﻿using System;

namespace Inu.Cate.Tms99
{
    internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            if (RightOperand.Register != null && LeftOperand.Register == null && IsOperatorExchangeable()) {
                ExchangeOperands();
            }

            ResultFlags |= Flag.Z;
            if (RightOperand is IntegerOperand integerOperand) {
                var value = OperatorId switch
                {
                    '+' => integerOperand.IntegerValue,
                    '-' => -integerOperand.IntegerValue,
                    _ => throw new NotImplementedException()
                };

                Tms99.ByteOperation.OperateConstant(this, "ai", DestinationOperand, LeftOperand, ByteRegister.ByteConst(value));
                return;
            }
            {
                var operation = OperatorId switch
                {
                    '+' => "ab",
                    '-' => "sb",
                    _ => throw new NotImplementedException()
                };
                Tms99.ByteOperation.Operate(this, operation, DestinationOperand, LeftOperand, RightOperand);
            }
        }

        protected override int Threshold() => 2;

        protected override void Increment(int count)
        {
            void ViaRegister(Cate.ByteRegister r)
            {
                r.Load(this, LeftOperand);
                r.Operate(this, "inc", true, count);
            }

            if (DestinationOperand.Register is ByteRegister byteRegister) {
                ViaRegister(byteRegister);
                return;
            }

            using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
            ViaRegister(reservation.ByteRegister);
            reservation.ByteRegister.Store(this, DestinationOperand);
        }

        protected override void Decrement(int count)
        {
            void ViaRegister(Cate.ByteRegister r)
            {
                r.Load(this, LeftOperand);
                r.Operate(this, "dec", true, count);
            }

            if (DestinationOperand.Register is ByteRegister byteRegister) {
                ViaRegister(byteRegister);
                return;
            }

            using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
            ViaRegister(reservation.ByteRegister);
            reservation.ByteRegister.Store(this, DestinationOperand);
        }
    }
}
