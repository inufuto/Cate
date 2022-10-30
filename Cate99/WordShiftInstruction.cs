using System;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftConstant(int count)
        {
            count &= 15;
            var operation = Operation();
            WordOperation.UsingAnyRegister(this, DestinationOperand, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                if (count > 0) {
                    WriteLine("\t" + operation + "\t" + temporaryRegister.Name + "," + count);
                }
                temporaryRegister.Store(this, DestinationOperand);
            });
        }

        private string Operation()
        {
            var operation = OperatorId switch
            {
                Keyword.ShiftLeft => "sla",
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => "sra",
                Keyword.ShiftRight => "srl",
                _ => throw new NotImplementedException()
            };
            return operation;
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            var operation = Operation();

            var r0 = ByteRegister.FromIndex(0);
            var candidates = WordRegister.Registers.Where(r => !r.Conflicts(r0)).ToList();
            WordOperation.UsingAnyRegister(this, candidates, DestinationOperand, LeftOperand, leftRegister =>
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

        public override bool IsCalling() => true;
    }
}
