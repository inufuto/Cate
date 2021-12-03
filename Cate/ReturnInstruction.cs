using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class ReturnInstruction : Instruction
    {
        public readonly Operand? SourceOperand;
        public readonly Anchor Anchor;

        protected ReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor) : base(function)
        {
            SourceOperand = sourceOperand;
            Anchor = anchor;
            Anchor.AddOriginAddress(function.NextAddress);

            SourceOperand?.AddUsage(function.NextAddress, Variable.Usage.Read);
        }

        public override string ToString()
        {
            return "return " + SourceOperand;
        }

        protected override Register? ResultRegister => SourceOperand != null ? Compiler.Instance.ReturnRegister(SourceOperand.Type.ByteCount) : null;

        public override bool IsJump()
        {
            return Function.Instructions.Last() != this;
        }

        public override void AddSourceRegisters()
        {
            if (SourceOperand != null) {
                AddSourceRegister(SourceOperand);
            }
        }

        //public override void RemoveDestinationRegister() { }

        protected void LoadResult()
        {
            if (SourceOperand == null)
                return;
            var register = Compiler.Instance.ReturnRegister(SourceOperand.Type.ByteCount);
            switch (register) {
                case ByteRegister byteRegister:
                    byteRegister.Load(this, SourceOperand);
                    break;
                case WordRegister wordRegister:
                    wordRegister.Load(this, SourceOperand);
                    break;
            }
        }
    }
}