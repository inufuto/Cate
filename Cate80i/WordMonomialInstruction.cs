using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Cate.I8080
{
    internal class WordMonomialInstruction : MonomialInstruction
    {
        public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            WordOperation.UsingAnyRegister(this, WordRegister.Registers, DestinationOperand, SourceOperand, wordRegister =>
            {
                wordRegister.Load(this, SourceOperand);
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    WriteLine("\tmov\ta," + wordRegister.Low);
                    WriteLine("\tcma");
                    WriteLine("\tmov\t" + wordRegister.Low + ",a");
                    WriteLine("\tmov\ta," + wordRegister.High);
                    WriteLine("\tcma");
                    WriteLine("\tmov\t" + wordRegister.High + ",a");
                });
                if (OperatorId == '-') {
                    WriteLine("\tinx\t" + wordRegister);
                }
                wordRegister.Store(this, DestinationOperand);
            });
        }
    }
}
