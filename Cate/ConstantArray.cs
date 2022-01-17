using System;
using System.Collections.Generic;
using System.IO;

namespace Inu.Cate
{
    public class ConstantArray : Constant
    {
        public readonly Constant[] ElementValues;

        public ConstantArray(ArrayType type, List<Constant> elementValues) : base(type)
        {
            var elementCount = type.ElementCount ?? elementValues.Count;
            ElementValues = new Constant[elementCount];
            for (var i = 0; i < elementCount; ++i) {
                ElementValues[i] = i < elementValues.Count ? elementValues[i] : type.ElementType.DefaultValue();
            }
        }

        public new ArrayType Type => (ArrayType)base.Type;

        public override void WriteAssembly(StreamWriter writer)
        {
            var size = 0;
            foreach (var elementValue in ElementValues) {
                elementValue.WriteAssembly(writer);
                size += elementValue.Type.ByteCount;
            }
        }

        public override Operand ToOperand()
        {
            throw new NotImplementedException();
        }
    }
}