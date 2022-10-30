using System;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class ByteShiftInstruction : Cate.ByteShiftInstruction
    {
        public ByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftVariable(Operand counterOperand)
        {
            var operation = Operation();

            var r0 = ByteRegister.FromIndex(0);
            var candidates = ByteRegister.Registers.Where(r => !r.Conflicts(r0)).ToList();
            ByteOperation.UsingAnyRegister(this, candidates, DestinationOperand, LeftOperand, leftRegister =>
            {
                leftRegister.Load(this, LeftOperand);
                ByteOperation.UsingRegister(this, r0, RightOperand, () =>
                {
                    r0.Load(this, RightOperand);
                    WriteLine("\tsrl\tr0,8");
                    WriteLine("\t" + operation + "\t" + leftRegister + ",r0");
                });
                leftRegister.Store(this, DestinationOperand);
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
