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
                using var reservation = ByteOperation.ReserveAnyRegister(this, DestinationOperand, LeftOperand);
                var temporaryRegister = reservation.ByteRegister;
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, operation, true, RightOperand);
                temporaryRegister.Store(this, DestinationOperand);
                AddChanged(temporaryRegister);
                return;
            }
            using (var reservation = WordOperation.ReserveAnyRegister(this, DestinationOperand, LeftOperand)) {
                var temporaryRegister = reservation.WordRegister;
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, operation, true, RightOperand);
                temporaryRegister.Store(this, DestinationOperand);
                AddChanged(temporaryRegister);
            }
        }
    }
}
