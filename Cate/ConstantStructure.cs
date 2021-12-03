using System;
using System.Collections.Generic;
using System.IO;

namespace Inu.Cate
{
    public class ConstantStructure : Constant
    {
        public readonly Constant[] MemberValues;
        public ConstantStructure(StructureType type, List<Constant> memberValues) : base(type)
        {
            MemberValues = new Constant[type.Members.Count];
            for (var i = 0; i < MemberValues.Length; ++i) {
                MemberValues[i] = i < memberValues.Count ? memberValues[i] : type.Members[i].Type.DefaultValue();
            }
        }

        public new StructureType Type => (StructureType)base.Type;

        public override void WriteAssembly(StreamWriter writer)
        {
            foreach (var memberValue in MemberValues) {
                memberValue.WriteAssembly(writer);
            }
        }

        public override Operand ToOperand()
        {
            throw new NotImplementedException();
        }
    }
}