using System;

namespace Inu.Cate.Mc6809
{
    internal class WordMonomialInstruction : MonomialInstruction
    {
        public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand)
        { }

        public override void BuildAssembly()
        {
            string operation = OperatorId switch
            {
                '-' => "neg",
                '~' => "com",
                _ => throw new NotImplementedException()
            };

            WordOperation.UsingRegister(this, WordRegister.D, () =>
            {
                WordRegister.D.Load(this, SourceOperand);
                WordRegister.D.Operate(this, operation, 1);
                WordRegister.D.Store(this, DestinationOperand);
            });
        }
    }
}