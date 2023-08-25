using System;
using System.Linq;

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
            var candidates = ByteRegister.Registers.Where(r=>!Equals(r, r0)).ToList();
            using (var reservation = ByteOperation.ReserveAnyRegister(this,candidates, RightOperand)) {
                void CallShift()
                {
                    r0.Load(this, LeftOperand);
                    r1.CopyFrom(this, reservation.ByteRegister);
                    r1.Expand(this, ((IntegerType)RightOperand.Type).Signed);
                    Compiler.CallExternal(this, functionName);
                    WriteLine("\tandi\tr0,>ff00");
                    RemoveRegisterAssignment(r0);
                    AddChanged(r0);
                    r0.Store(this, DestinationOperand);
                }
                reservation.ByteRegister.Load(this, RightOperand);
                if (Equals(DestinationOperand.Register, r0)) {
                    CallShift();
                }
                else {
                    using (ByteOperation.ReserveRegister(this, r0)) {
                        CallShift();
                    }
                }
            }
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

            void ViaRegister(Cate.ByteRegister r)
            {
                r.Load(this, LeftOperand);
                if (count > 0) {
                    WriteLine("\t" + operation + "\t" + r.Name + "," + count);
                    if (OperatorId == Keyword.ShiftRight) {
                        WriteLine("\tandi\t" + r.Name + ",>ff00");
                    }
                }
                AddChanged(r);
            }

            if (DestinationOperand.Register is ByteRegister byteRegister) {
                ViaRegister(byteRegister);
                return;
            }

            using var reservation = ByteOperation.ReserveAnyRegister(this, LeftOperand);
            var register = reservation.ByteRegister;
            ViaRegister(register);
            register.Store(this, DestinationOperand);
        }
        public override bool IsCalling() => true;
    }
}
