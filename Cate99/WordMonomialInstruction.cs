using System;

namespace Inu.Cate.Tms99
{
    internal class WordMonomialInstruction : MonomialInstruction
    {
        public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            var operation = OperatorId switch
            {
                '-' => "neg",
                '~' => "inv",
                _ => throw new NotImplementedException()
            };
            Tms99.WordOperation.Operate(this,operation, DestinationOperand,SourceOperand);
        }
    }
}
