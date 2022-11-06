using System;
using System.Collections.Generic;
using System.IO;

namespace Inu.Cate
{
    public class ConstantStructure : Constant
    {
        public class MemberValue
        {
            public readonly StructureType.Member Member;
            public readonly Constant Value;

            public MemberValue(StructureType.Member member, Constant value)
            {
                Member = member;
                Value = value;
            }
        }
        public readonly MemberValue[] MemberValues;
        public ConstantStructure(StructureType type, IReadOnlyList<Constant> memberValues) : base(type)
        {
            MemberValues = new MemberValue[type.Members.Count];
            for (var i = 0; i < MemberValues.Length; ++i) {
                var value = i < memberValues.Count ? memberValues[i] : type.Members[i].Type.DefaultValue();
                MemberValues[i] = new MemberValue(type.Members[i], value);
            }
        }

        public new StructureType Type => (StructureType)base.Type;

        public override void WriteAssembly(StreamWriter writer)
        {
            for (var i = 0; i < MemberValues.Length; i++) {
                var memberValue = MemberValues[i];
                memberValue.Value.WriteAssembly(writer);
                if (i >= MemberValues.Length - 1) continue;
                var offset = MemberValues[i].Member.Offset;
                var nextOffset = MemberValues[i + 1].Member.Offset;
                Compiler.Instance.WriteAlignment(writer, nextOffset - (offset + MemberValues[i].Member.Type.ByteCount));
            }
        }

        public override Operand ToOperand()
        {
            throw new NotImplementedException();
        }
    }
}