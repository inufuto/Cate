using System.Diagnostics;
using Inu.Language;

namespace Inu.Cate
{
    class Dereference : AssignableValue
    {
        private readonly Value sourceValue;
        private readonly int offset;
        private Variable? variable;

        public Dereference(Type type, Value sourceValue, int offset = 0) : base(type)
        {
            this.sourceValue = sourceValue;
            this.offset = offset;
            Debug.Assert(sourceValue.Type is PointerType);
        }

        private void UpdateVariable(Function function)
        {
            if (variable != null) return;

            if (sourceValue is VariableValue sourceVariableValue) {
                variable = sourceVariableValue.Variable;
                return;
            }
            variable = function.CreateTemporaryVariable(new PointerType(Type));
            //var assignableOperand = temporaryVariable.ToAssignableOperand(function, Variable.Usage.Write, temporaryVariable.Type, offset);
            sourceValue.BuildInstructions(function, variable.ToAssignableOperand());
        }

        public override Operand ToOperand(Function function)
        {
            return ToAssignableOperand(function);
        }

        public override bool CanAssign()
        {
            return true;
        }

        public override AssignableOperand ToAssignableOperand(Function function)
        {
            UpdateVariable(function);
            Debug.Assert(variable != null);
            Debug.Assert(variable.Type is PointerType);
            return new IndirectOperand(variable, offset * ((PointerType)variable.Type).ElementType.ByteCount);
        }

        public override Value? Reference(SourcePosition position)
        {
            return offset != 0 ? sourceValue.BinomialResult(position, '+', new ConstantInteger(offset)) : sourceValue;
        }
    }
}