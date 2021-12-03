using System.Diagnostics;
using Inu.Language;

namespace Inu.Cate
{
    class StructureMember : AssignableValue
    {
        private AssignableValue assignableValue;
        private int offset;

        public StructureMember(Type type, AssignableValue assignableValue, int offset) : base(type)
        {
            this.assignableValue = assignableValue;
            this.offset = offset;
        }

        public override bool CanAssign()
        {
            return assignableValue.CanAssign();
        }

        public override AssignableOperand ToAssignableOperand(Function function)
        {
            return assignableValue.ToAssignableOperand(function).ToMember(Type, offset);
        }

        public override Value? Reference(SourcePosition position)
        {
            Debug.Assert(assignableValue != null);
            var structurePointer = assignableValue.Reference(position);
            var bytePointer = structurePointer!.CastTo(new PointerType(IntegerType.ByteType));
            var addedBytePointer = bytePointer!.BinomialResult(position, '+', new ConstantInteger(offset));
            return addedBytePointer!.CastTo(new PointerType(Type));
        }

        public override Operand ToOperand(Function function)
        {
            return ToAssignableOperand(function);
        }

    }
}
