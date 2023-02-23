using System.Collections.Generic;

namespace Inu.Cate.MuCom87
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType) { }

        protected override void ExpandSigned()
        {
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, SourceOperand);
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                WriteLine("\tshal");
                var candidates = ByteOperation.RegistersOtherThan(ByteRegister.A);
                ByteOperation.UsingAnyRegister(this, candidates, temporary =>
                {
                    temporary.CopyFrom(this, ByteRegister.A);
                    WriteLine("\tsbb\ta," + temporary.Name);
                });
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            });
        }
    }
}
