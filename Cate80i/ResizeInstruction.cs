using System;

namespace Inu.Cate.I8080
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType) { }

        protected override void ExpandSigned()
        {
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, SourceOperand);
                WordOperation.UsingRegister(this, WordRegister.Hl, () =>
                {
                    Compiler.CallExternal(this, "cate.ExpandSigned");
                    WordRegister.Hl.Store(this, DestinationOperand);
                });
            });
        }
    }
}
