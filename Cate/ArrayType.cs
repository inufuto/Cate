using System;
using System.Collections.Generic;

namespace Inu.Cate
{
    public class ArrayType : Type
    {
        public readonly Type ElementType;
        public readonly int? ElementCount;

        public ArrayType(Type elementType, int? elementCount)
        {
            ElementType = elementType;
            ElementCount = elementCount;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is ArrayType arrayType) || !ElementType.Equals(arrayType.ElementType)) return false;
            if (ElementCount != null) {
                return arrayType.ElementCount != null && ElementCount == arrayType.ElementCount;
            }
            return ElementCount==null || arrayType.ElementCount == null;
        }

        public override int GetHashCode()
        {
            var hashCode = ElementType.GetHashCode();
            if (ElementCount != null) {
                hashCode += ElementCount.GetHashCode();
            }
            return hashCode;
        }

        public override int ByteCount => ElementCount != null ? ElementType.ByteCount * ElementCount.Value : 0;
        public override int Incremental => throw new NotImplementedException();
        public override int MaxElementSize => ElementType.MaxElementSize;

        public override Constant DefaultValue()
        {
            var elementValues = new List<Constant>();
            if (ElementCount != null) {
                var elementValue = ElementType.DefaultValue();
                for (var i = 0; i < ElementCount; ++i) {
                    elementValues.Add(elementValue);
                }
            }
            return new ConstantArray(this, elementValues);
        }

        public override Constant? ParseConstant(Compiler compiler)
        {
            return compiler.ParseConstantArray(this);
        }

        public PointerType ToPointerType()
        {
            return new PointerType(ElementType);
        }
    }
}