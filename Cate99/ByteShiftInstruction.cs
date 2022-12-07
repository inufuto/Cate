using System;

namespace Inu.Cate.Tms99
{
    internal class ByteShiftInstruction : Cate.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftVariable(Operand counterOperand)
        {
            var functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeft",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSigned"
                    : "cate.ShiftRight",
                _ => throw new NotImplementedException()
            };
            var r0 = ByteRegister.FromIndex(0);
            var r1 = ByteRegister.FromIndex(1);
            ByteOperation.UsingRegister(this, r1, () =>
            {
                void CallShift()
                {
                    r0.Load(this, LeftOperand);
                    r1.Expand(this, ((IntegerType)RightOperand.Type).Signed);
                    Compiler.CallExternal(this, functionName);
                    WriteLine("\tandi\tr0,>ff00");
                    RemoveRegisterAssignment(r0);
                    ChangedRegisters.Add(r0);
                    r0.Store(this, DestinationOperand);
                }
                r1.Load(this, RightOperand);
                if (Equals(DestinationOperand.Register, r0)) {
                    CallShift();
                }
                else {
                    ByteOperation.UsingRegister(this, r0, CallShift);
                }
            });
        }

        protected override string Operation()
        {
            return OperatorId switch
            {
                Keyword.ShiftLeft => "sla",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed ? "sra" : "srl",
                _ => throw new NotImplementedException()
            };
        }

        protected override void ShiftConstant(int count)
        {
            var operation = Operation();
            ByteOperation.UsingAnyRegisterToChange(this, DestinationOperand, LeftOperand, register =>
            {
                register.Load(this, LeftOperand);
                if (count > 0) {
                    WriteLine("\t" + operation + "\t" + register.Name + "," + count);
                    if (OperatorId == Keyword.ShiftRight) {
                        WriteLine("\tandi\t" + register.Name + ",>ff00");
                    }
                }
                register.Store(this, DestinationOperand);
                ChangedRegisters.Add(register);
            });
        }
        public override bool IsCalling() => true;
    }
}
