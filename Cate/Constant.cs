using System.IO;

namespace Inu.Cate
{
    public abstract class Constant : Value
    {
        protected Constant(Type type) : base(type) { }

        public override bool IsConstant() => true;

        public abstract void WriteAssembly(StreamWriter writer);
        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            var instruction = Compiler.Instance.CreateLoadInstruction(function, destinationOperand, ToOperand());
            function.Instructions.Add(instruction);
        }

        public override void BuildInstructions(Function function) { }

        public abstract Operand ToOperand();

        public override Operand ToOperand(Function function)
        {
            return ToOperand();
        }
    }
}