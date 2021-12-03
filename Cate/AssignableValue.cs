using Inu.Language;

namespace Inu.Cate
{
    public abstract class AssignableValue : Value
    {
        protected AssignableValue(Type type) : base(type)
        { }

        public override Operand ToOperand(Function function)
        {
            return ToAssignableOperand(function);
        }

        public override void BuildInstructions(Function function, AssignableOperand destinationOperand)
        {
            var sourceOperand = ToOperand(function);
            var instruction = Compiler.Instance.CreateLoadInstruction(function, destinationOperand, sourceOperand);
            function.Instructions.Add(instruction);
        }

        public override void BuildInstructions(Function function) { }

        public abstract bool CanAssign();
        public abstract AssignableOperand ToAssignableOperand(Function function);
        public abstract Value? Reference(SourcePosition position);
    }
}