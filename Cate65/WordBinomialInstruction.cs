using System;

namespace Inu.Cate.Mos6502
{
    internal class WordBinomialInstruction : BinomialInstruction
    {
        public WordBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }


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
                case '+':
                    lowOperation = "clc|adc";
                    highOperation = "adc";
                    break;
                case '-':
                    lowOperation = "sec|sbc";
                    highOperation = "sbc";
                    break;
                default:
                    throw new NotImplementedException();
            }
            Cate.Compiler.Instance.ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, lowOperation, true, Compiler.LowByteOperand(RightOperand));
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, highOperation, true, Compiler.HighByteOperand(RightOperand));
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            });
        }
    }
}