using System.Diagnostics;
using System.Linq;
using Inu.Language;

namespace Inu.Cate
{
    public abstract class CompareInstruction : Instruction
    {
        public readonly int OperatorId;
        public readonly Operand LeftOperand;
        public readonly Operand RightOperand;
        public readonly Anchor Anchor;
        public readonly bool Signed;

        protected CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand,
            Anchor anchor) : base(function)
        {
            OperatorId = operatorId;
            LeftOperand = leftOperand;
            RightOperand = rightOperand;
            Anchor = anchor;
            Anchor.AddOriginAddress(function.NextAddress);
            Debug.Assert(leftOperand.Type.Equals(rightOperand.Type));
            if (leftOperand.Type is IntegerType integerType) {
                Signed = integerType.Signed;
            }
            else {
                Signed = false;
            }
            LeftOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
            rightOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
        }

        public override string ToString() =>
            "if " + LeftOperand + " " + ReservedWord.FromId(OperatorId) + " " +
            RightOperand + " goto " + Anchor;

        public override bool IsJump() => true;

        public override void BuildAssembly()
        {
            if (LeftOperand.Type.ByteCount == 1) {
                CompareByte();
            }
            else {
                CompareWord();
            }
        }

        protected abstract void CompareByte();

        protected abstract void CompareWord();

        protected bool CanOmitOperation(Flag flag)
        {
            return PreviousInstructions.All(
                instruction => instruction.ResultFlags.HasFlag(flag) &&
                instruction.ResultOperand != null && instruction.ResultOperand.Equals(LeftOperand)
            );
        }

        public override void AddSourceRegisters()
        {
            AddSourceRegister(LeftOperand);
            AddSourceRegister(RightOperand);
        }

        //public override void RemoveDestinationRegister() { }
    }
}
