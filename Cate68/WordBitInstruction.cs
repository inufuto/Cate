using System;

namespace Inu.Cate.Mc6800
{
    internal class WordBitInstruction : BinomialInstruction
    {
        public WordBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand,
            Operand rightOperand)
            : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }


        public override void BuildAssembly()
        {
            string lowOperation, highOperation;
            switch (OperatorId) {
                case '|':
                    lowOperation = highOperation = "ora";
                    break;
                case '^':
                    lowOperation = highOperation = "eor";
                    break;
                case '&':
                    lowOperation = highOperation = "and";
                    break;
                default:
                    throw new NotImplementedException();
            }

            OperatePair(lowOperation, highOperation);
        }

        private void OperatePair(string lowOperation, string highOperation)
        {
            Mc6800.WordOperation.OperatePair(this, LeftOperand, RightOperand, DestinationOperand, lowOperation, highOperation);
        }
    }
}