using System;
using System.Collections.Generic;
using System.IO;

namespace Inu.Cate;

public class ConstantStructure : Constant
{
    public class MemberValue(StructureType.Member member, Constant value)
    {
        public readonly StructureType.Member Member = member;
        public readonly Constant Value = value;
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
        var totalSize = 0;
        for (var i = 0; i < MemberValues.Length; i++) {
            var memberValue = MemberValues[i];
            memberValue.Value.WriteAssembly(writer);
            var memberByteCount = MemberValues[i].Member.Type.ByteCount;
            totalSize += memberByteCount;
            if (i >= MemberValues.Length - 1) continue;
            var offset = MemberValues[i].Member.Offset;
            var nextOffset = MemberValues[i + 1].Member.Offset;
            var alignmentByteCount = nextOffset - (offset + memberByteCount);
            Compiler.WriteAlignment(writer, alignmentByteCount);
            totalSize += alignmentByteCount;
        }
        Compiler.WriteAlignment(writer, Type.ByteCount-totalSize);
    }

    public override Operand ToOperand()
    {
        throw new NotImplementedException();
    }
}