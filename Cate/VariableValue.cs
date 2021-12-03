using Inu.Language;

namespace Inu.Cate
{
    class VariableValue : AssignableValue
    {
        public readonly Variable Variable;

        public VariableValue(Variable variable) : base(variable.Type)
        {
            Variable = variable;
        }

        public override Operand ToOperand(Function function)
        {
            return Variable.ToOperand();
        }

        public override AssignableOperand ToAssignableOperand(Function function)
        {
            return Variable.ToAssignableOperand();
        }

        public override Value? Reference(SourcePosition position)
        {
            return new ConstantPointer(new PointerType(Type), Variable, 0);
        }

        public override bool CanAssign()
        {
            return !Variable.IsConstant();
        }
    }
}