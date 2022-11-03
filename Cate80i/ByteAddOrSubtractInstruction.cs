using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Inu.Cate.I8080
{
    internal class ByteAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand,
            rightOperand)
        {
            Debug.Assert(destinationOperand.Type.ByteCount == 1);
        }

        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            if (
                Equals(register, ByteRegister.A) &&
                !IsOperatorExchangeable() &&
                RightOperand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)
            )
                return false;
            return base.CanAllocateRegister(variable, register);
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
                '+' => "add|adi",
                '-' => "sub|sui",
                _ => throw new NotImplementedException()
            };
            ResultFlags |= Flag.Z;

            if (Equals(RightOperand.Register, ByteRegister.A)) {
                var candidates = ByteRegister.Registers.Where(r => !Equals(r, ByteRegister.A)).ToList();
                ByteOperation.UsingAnyRegister(this, candidates, byteRegister =>
                {
                    byteRegister.CopyFrom(this, ByteRegister.A);
                    ByteRegister.A.Load(this, LeftOperand);
                    WriteLine("\t" + operation.Split('|')[0] + "\t" + byteRegister);
                    ByteRegister.A.Store(this, DestinationOperand);
                    ChangedRegisters.Add(ByteRegister.A);
                    ByteRegister.A.CopyFrom(this, byteRegister);
                });
                return;
            }


            ByteOperation.UsingRegister(this, ByteRegister.A, LeftOperand, () =>
            {
                ByteRegister.A.Load(this, LeftOperand);
                ByteRegister.A.Operate(this, operation, true, RightOperand);
                ByteRegister.A.Store(this, DestinationOperand);
                ChangedRegisters.Add(ByteRegister.A);
            });
        }

        protected override int Threshold()
        {
            return LeftOperand.Register == null || Equals(LeftOperand.Register, ByteRegister.A) ? 1 : 4;
        }

        protected override void Increment(int count)
        {
            OperateByte("inr", count);
            ResultFlags |= Flag.Z;
        }

        protected override void Decrement(int count)
        {
            OperateByte("dcr", count);
            ResultFlags |= Flag.Z;
        }
    }
}
