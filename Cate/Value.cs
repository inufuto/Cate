using Inu.Language;

namespace Inu.Cate
{
    public abstract class Value
    {
        public readonly Type Type;

        protected Value(Type type)
        {
            Type = type;
        }

        public virtual bool IsConstant() => false;

        public virtual Value? BinomialResult(SourcePosition position, int operatorId, Value rightValue)
        {
            return Type.BinomialResult(position, operatorId, this, rightValue);
        }

        public virtual Value? MonomialResult(SourcePosition position, int operatorId)
        {
            return Type.MonomialResult(position, operatorId, this);
        }

        public virtual Operand ToOperand(Function function)
        {
            var variable = function.CreateTemporaryVariable(Type);
            BuildInstructions(function, variable.ToAssignableOperand());
            return variable.ToOperand();
        }

        public abstract void BuildInstructions(Function function, AssignableOperand destinationOperand);
        public abstract void BuildInstructions(Function function);

        public virtual Value? ConvertTypeTo(Type type)
        {
            return Type.Equals(type) ? this : Type.ConvertType(this, type);
        }

        public virtual Value? CastTo(Type type)
        {
            var value = ConvertTypeTo(type);
            if (value != null) {
                return value;
            }
            return Type.Cast(this, type);
        }

        public virtual BooleanValue? ToBooleanValue()
        {
            if (Type.Equals(BooleanType.Type)) {
                return new Comparison(Keyword.NotEqual, this, new ConstantBoolean(false));
            }
            return null;
        }

        public Compiler Compiler => Compiler.Instance;
    }
}
