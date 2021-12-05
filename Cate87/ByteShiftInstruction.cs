using System;

namespace Inu.Cate.MuCom87
{
    class ByteShiftInstruction : Cate.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            if (Equals(LeftOperand.Register, ByteRegister.C) && Equals(DestinationOperand.Register, ByteRegister.C) && RightOperand is IntegerOperand integerOperand && !((IntegerType)LeftOperand.Type).Signed) {
                string operation = OperatorId switch
                {
                    Keyword.ShiftLeft => "shcl",
                    Keyword.ShiftRight => "shcr",
                    _ => throw new NotImplementedException()
                };
                for (var i = 0; i < integerOperand.IntegerValue; ++i) {
                    WriteLine("\t" + operation);
                }
                return;
            }
            base.BuildAssembly();
        }

        protected override void ShiftConstant(int count)
        {
            if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
                ByteOperation.UsingRegister(this, ByteRegister.B, () =>
                {
                    ByteRegister.B.LoadConstant(this, count);
                    CallExternal("cate.ShiftRightSignedA");
                });
                return;
            }

            var operation = Operation();
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, LeftOperand);
                for (var i = 0; i < count; ++i) {
                    WriteLine("\t" + operation);
                }
                ByteRegister.A.Store(this, DestinationOperand);
            });
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            string functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftA",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedA"
                    : "cate.ShiftRightA",
                _ => throw new NotImplementedException()
            };
            ByteOperation.UsingRegister(this, ByteRegister.B, () =>
            {
                ByteRegister.B.Load(this, RightOperand);
                CallExternal(functionName);
            });
        }

        private void CallExternal(string functionName)
        {
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, LeftOperand);
                Compiler.CallExternal(this, functionName);
                RemoveVariableRegister(ByteRegister.A);
                ChangedRegisters.Add(ByteRegister.A);
                RemoveVariableRegister(ByteRegister.B);
                ChangedRegisters.Add(ByteRegister.B);
                ByteRegister.A.Store(this, DestinationOperand);
            });
        }

        protected override string Operation()
        {
            return OperatorId switch
            {
                Keyword.ShiftLeft => "shal",
                Keyword.ShiftRight when !((IntegerType)LeftOperand.Type).Signed => "shar",
                _ => throw new NotImplementedException()
            };
        }
    }
}
