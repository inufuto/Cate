using System;

namespace Inu.Cate.I8080
{
    internal class WordBitInstruction : BinomialInstruction
    {
        public WordBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override void BuildAssembly()
        {
            var operation = OperatorId switch
            {
                '|' => "ora|ori",
                '^' => "xra|xri",
                '&' => "ana|ani",
                _ => throw new NotImplementedException()
            };
            Cate.Compiler.Instance.ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, operation, true, Compiler.LowByteOperand(RightOperand));
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, operation, true, Compiler.HighByteOperand(RightOperand));
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            });
        }
    }
}
