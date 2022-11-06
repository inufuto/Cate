using System.IO;
using Inu.Language;

namespace Inu.Cate
{
    public class ConstantPointer : Constant
    {
        public readonly Variable? Variable;
        public readonly int Offset;
        public readonly int? ElementCount;

        public ConstantPointer(PointerType type, Variable? variable, int offset, int? elementCount = null) : base(type)
        {
            Variable = variable;
            Offset = offset;
            ElementCount = elementCount;
        }

        public new PointerType Type => (PointerType)base.Type;

        public override Value? BinomialResult(SourcePosition position, int operatorId, Value rightValue)
        {
            if (rightValue is ConstantInteger rightConstant) {
                switch (operatorId) {
                    case '+':
                        return new ConstantPointer(Type, Variable, Offset + rightConstant.IntegerValue);
                    case '-':
                        return new ConstantPointer(Type, Variable, Offset - rightConstant.IntegerValue);
                }
            }
            return base.BinomialResult(position, operatorId, rightValue);
        }

        public override Value? CastTo(Type type)
        {
            if (type is PointerType pointerType) {
                var offsetInBytes = Offset * Type.ElementType.ByteCount;
                return new ConstantPointer(pointerType, Variable, offsetInBytes / pointerType.ElementType.ByteCount);
            }
            return base.CastTo(type);
        }

        public override void WriteAssembly(StreamWriter writer)
        {
            if (Variable != null) {
                writer.Write("\tdefw " + Variable.Label);
                if (Offset != 0) {
                    writer.Write(" + " + Type.ElementType.ByteCount * Offset);
                }

                writer.WriteLine();
            }
            else {
                writer.WriteLine("\tdefw 0");
            }
        }

        public override Operand ToOperand()
        {
            if (Variable == null) return new IntegerOperand(Type, 0);
            Variable.Static = true;
            return new PointerOperand(Type, Variable, Offset * Type.ElementType.ByteCount);
        }
    }

    public class NullPointerOperand : IntegerOperand
    {
        public NullPointerOperand() : base(new PointerType(VoidType.Type), 0) { }
    }

    class NullPointer : Constant
    {

        public NullPointer(PointerType type) : base(type) { }

        public NullPointer() : this(new PointerType(VoidType.Type)) { }

        public override void WriteAssembly(StreamWriter writer)
        {
            writer.WriteLine("\tdefw " + 0);
        }

        public override Operand ToOperand()
        {
            return new NullPointerOperand();
        }

        public override Value? ConvertTypeTo(Type type)
        {
            if (type is PointerType pointerType) {
                return new NullPointer(pointerType);
            }
            return base.ConvertTypeTo(type);
        }
    }

    class VoidType : Type
    {
        public static readonly VoidType Type = new VoidType();

        public override int ByteCount => 0;

        public override bool IsVoid() => true;

        public override int Incremental => throw new System.NotImplementedException();
        public override int MaxElementSize => ByteCount;

        public override Constant DefaultValue()
        {
            throw new System.NotImplementedException();
        }

        public override Constant? ParseConstant(Compiler compiler)
        {
            throw new System.NotImplementedException();
        }
    }
}