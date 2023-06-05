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
            using (ByteOperation.ReserveRegister(this, ByteRegister.Cl, counterOperand)) {
                ByteRegister.Cl.Load(this, counterOperand);
                //CancelOperandRegister(counterOperand);
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
                void ViaRegister(Cate.ByteRegister r)
                {
                    r.Load(this, LeftOperand);
                    if (count != null) {
                        for (var i = 0; i < count; ++i) {
                            WriteLine("\t" + operation + r + ",1");
                        }
                    }
                    else {
                        WriteLine("\t" + operation + r + ",cl");
                    }

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
                    if (count != null) {
                        for (var i = 0; i < count; ++i) {
                            WriteLine("\t" + operation + r + ",1");
                        }
                    }
                    else {
                        WriteLine("\t" + operation + r + ",cl");
                    }

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
