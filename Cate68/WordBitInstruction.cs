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
            var operation = OperatorId switch
            {
                '|' => "ora",
                '^' => "eor",
                '&' => "and",
                _ => throw new NotImplementedException()
            };
            OperatePair(operation);
        }

        private void OperatePair(string operation)
        {
            Mc6800.WordOperation.OperatePair(this, LeftOperand, RightOperand, DestinationOperand, operation, operation);
        }
    }
}