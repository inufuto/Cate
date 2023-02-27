using System;

namespace Inu.Cate.Mc6800
{
    internal class WordAddOrSubtractInstruction : Cate.AddOrSubtractInstruction
    {
        public WordAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        protected override int Threshold() => 8;

        public override void BuildAssembly()
        {
            if (IncrementOrDecrement())
                return;

            var functionName = OperatorId switch
            {
                '+' => "Cate.AddX",
                '-' => "Cate.SubX",
                _ => throw new NotImplementedException()
            };

            if (RightOperand is IntegerOperand integerOperand) {
                var rightConstant = integerOperand.IntegerValue;
                if (rightConstant >= 0 && rightConstant < 0x100) {
                    void AddByte(Cate.ByteRegister byteRegister)
                    {
                        functionName += byteRegister.Name.ToUpper();
                        WordRegister.X.Load(this, LeftOperand);
                        byteRegister.LoadConstant(this, rightConstant);
                        Compiler.CallExternal(this, functionName);
                        WordRegister.X.Store(this, DestinationOperand);
                    }

                    if (!IsRegisterReserved(ByteRegister.A)) {
                        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                            AddByte(ByteRegister.A);
                            RemoveRegisterAssignment(ByteRegister.A);
                        }
                        return;
                    }
                    if (!IsRegisterReserved(ByteRegister.B)) {
                        using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                            AddByte(ByteRegister.B);
                            RemoveRegisterAssignment(ByteRegister.B);
                        }
                        return;
                    }

                    using var reservation = ByteOperation.ReserveAnyRegister(this);
                    AddByte(reservation.ByteRegister);
                    return;
                }
            }
            functionName += "AB";
            WordRegister.X.Load(this, LeftOperand);
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                    Mc6800.Compiler.LoadPairRegister(this, RightOperand);
                    Compiler.CallExternal(this, functionName);
                }
            }
            WordRegister.X.Store(this, DestinationOperand);
        }

        protected override void Increment(int count)
        {
            IncrementOrDecrement("inx", count);
        }


        protected override void Decrement(int count)
        {
            IncrementOrDecrement("dex", count);
        }

        private void IncrementOrDecrement(string operation, int count)
        {
            WordRegister.X.Load(this, LeftOperand);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + operation);
            }
            WordRegister.X.Store(this, DestinationOperand);
        }
    }
}