using System;

namespace Inu.Cate.I8086
{
    internal class ShiftInstruction : Cate.ShiftInstruction
    {
        public ShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        protected override int Threshold() => 4;

        protected override void ShiftConstant(int count)
        {
            Operate(count);
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.Cl)) {
                ByteRegister.Cl.Load(this, counterOperand);
                CancelOperandRegister(counterOperand);
                Operate(null);
            }
        }

        private void Operate(int? count)
        {
            var operation = OperatorId switch
            {
                Keyword.ShiftLeft => "shl ",
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => "sar ",
                Keyword.ShiftRight => "shr ",
                _ => throw new NotImplementedException()
            };

            if (
                count is 1 &&
                DestinationOperand.SameStorage(LeftOperand) &&
                DestinationOperand is VariableOperand { Register: null } destinationVariableOperand
            ) {
                var size = DestinationOperand.Type.ByteCount == 1 ? "byte" : "word";
                var destinationAddress = destinationVariableOperand.MemoryAddress();
                WriteLine("\t" + operation + size + " ptr [" + destinationAddress + "],1");
                return;
            }
            if (DestinationOperand.Type.ByteCount == 1) {
                using var reservation = ByteOperation.ReserveAnyRegister(this, DestinationOperand, LeftOperand);
                var temporaryRegister = reservation.ByteRegister;
                temporaryRegister.Load(this, LeftOperand);
                if (count != null) {
                    for (var i = 0; i < count; ++i) {
                        WriteLine("\t" + operation + temporaryRegister + ",1");
                    }
                }
                else {
                    WriteLine("\t" + operation + temporaryRegister + ",cl");
                }
                temporaryRegister.Store(this, DestinationOperand);
                AddChanged(temporaryRegister);
                return;
            }

            using (var reservation = WordOperation.ReserveAnyRegister(this, DestinationOperand, LeftOperand)) {
                var temporaryRegister = reservation.WordRegister;
                temporaryRegister.Load(this, LeftOperand);
                if (count != null) {
                    for (var i = 0; i < count; ++i) {
                        WriteLine("\t" + operation + temporaryRegister + ",1");
                    }
                }
                else {
                    WriteLine("\t" + operation + temporaryRegister + ",cl");
                }
                temporaryRegister.Store(this, DestinationOperand);
                AddChanged(temporaryRegister);
            }
        }
    }
}
