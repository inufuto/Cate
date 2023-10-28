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

            Action load, save;
            if (LeftOperand.Type is PointerType)
            {
                load = () =>
                {
                    PointerRegister.X.Load(this, LeftOperand);
                };
            }
            else
            {
                load = () =>
                {
                    IndexRegister.X.Load(this, LeftOperand);
                };
            }
            if (DestinationOperand.Type is PointerType) {
                save = () =>
                {
                    PointerRegister.X.Store(this, DestinationOperand);
                };
            }
            else {
                save = () =>
                {
                    IndexRegister.X.Store(this, DestinationOperand);
                };
            }

            if (RightOperand is IntegerOperand integerOperand) {
                var rightConstant = integerOperand.IntegerValue;
                if (rightConstant is >= 0 and < 0x100) {
                    void AddByte(Cate.ByteRegister byteRegister)
                    {
                        functionName += byteRegister.Name.ToUpper();
                        load();
                        byteRegister.LoadConstant(this, rightConstant);
                        Compiler.CallExternal(this, functionName);
                        save();
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
            load();
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                    Mc6800.Compiler.LoadPairRegister(this, RightOperand);
                    Compiler.CallExternal(this, functionName);
                }
            }
            save();
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
            Action load, save;
            if (LeftOperand.Type is PointerType) {
                load = () =>
                {
                    PointerRegister.X.Load(this, LeftOperand);
                };
                save = () =>
                {
                    PointerRegister.X.Store(this, DestinationOperand);
                };
            }
            else {
                load = () =>
                {
                    IndexRegister.X.Load(this, LeftOperand);
                };
                save = () =>
                {
                    IndexRegister.X.Store(this, DestinationOperand);
                };
            }
            load();
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + operation);
            }
            save();
        }
    }
}