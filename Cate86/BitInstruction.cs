using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Cate.I8086
{
    internal class BitInstruction : BinomialInstruction
    {
        public BitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override void BuildAssembly()
        {
            if (IsOperatorExchangeable()) {
                if (LeftOperand is ConstantOperand || DestinationOperand.SameStorage(RightOperand)) {
                    ExchangeOperands();
                }
            }

            var operation = OperatorId switch
            {
                '|' => "or ",
                '^' => "xor ",
                '&' => "and ",
                _ => throw new NotImplementedException()
            };
            ResultFlags |= Flag.Z;

            if (DestinationOperand.SameStorage(LeftOperand) &&
                DestinationOperand is VariableOperand { Register: null } destinationVariableOperand) {
                var size = DestinationOperand.Type.ByteCount == 1 ? "byte" : "word";
                var destinationAddress = destinationVariableOperand.MemoryAddress();
                var value = RightOperand switch
                {
                    ConstantOperand constantOperand => constantOperand.MemoryAddress(),
                    VariableOperand { Register: { }, Offset: 0 } variableOperand => variableOperand.Register.Name,
                    _ => null
                };
                if (value != null) {
                    WriteLine("\t" + operation + " " + size + " ptr [" + destinationAddress + "]," + value);
                    return;
                }
            }
            if (DestinationOperand.Type.ByteCount == 1) {
                void ViaRegister(Cate.ByteRegister r)
                {
                    r.Load(this, LeftOperand);
                    r.Operate(this, operation, true, RightOperand);
                    AddChanged(r);
                }

                if (DestinationOperand.Register is ByteRegister byteRegister && !Equals(RightOperand.Register, byteRegister)) {
                    ViaRegister(byteRegister);
                    return;
                }
                using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
                ViaRegister(reservation.ByteRegister);
                reservation.ByteRegister.Store(this, DestinationOperand);
                return;
            }
            {
                void ViaRegister(Cate.WordRegister r)
                {
                    r.Load(this, LeftOperand);
                    r.Operate(this, operation, true, RightOperand);
                    AddChanged(r);
                }

                if (DestinationOperand.Register is WordRegister wordRegister && !Equals(RightOperand.Register, wordRegister)) {
                    ViaRegister(wordRegister);
                    return;
                }
                using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
                ViaRegister(reservation.WordRegister);
                reservation.WordRegister.Store(this, DestinationOperand);
            }
        }
    }
}
