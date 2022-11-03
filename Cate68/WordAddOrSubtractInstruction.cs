using System;
using System.Security.Cryptography.X509Certificates;

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

            string functionName = OperatorId switch
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

                    if (!IsRegisterInUse(ByteRegister.A)) {
                        ByteRegister.Using(this, ByteRegister.A, () =>
                        {
                            AddByte(ByteRegister.A);
                            RemoveRegisterAssignment(ByteRegister.A);
                        });
                        return;
                    }
                    if (!IsRegisterInUse(ByteRegister.B)) {
                        ByteRegister.Using(this, ByteRegister.B, () =>
                        {
                            AddByte(ByteRegister.B);
                            RemoveRegisterAssignment(ByteRegister.B);
                        });
                        return;
                    }
                    ByteOperation.UsingAnyRegister(this, AddByte);
                    return;
                }
            }
            functionName += "AB";
            WordRegister.X.Load(this, LeftOperand);
            ByteRegister.UsingPair(this, () =>
            {
                Mc6800.Compiler.LoadPairRegister(this, RightOperand);
                Compiler.CallExternal(this, functionName);
            });
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