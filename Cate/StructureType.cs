using System.Collections.Generic;
using System.Linq;
using Inu.Language;

namespace Inu.Cate
{
    public class StructureType : Type
    {
        public class Member
        {
            public readonly int Offset;
            public readonly Type Type;
            public readonly int Id;

            public Member(Type type, int id, int offset)
            {
                Id = id;
                Type = type;
                this.Offset = offset;
            }

            public override bool Equals(object? obj)
            {
                return obj is Member member && (Id == member.Id && Type.Equals(member.Type));
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() + Type.GetHashCode() + Offset.GetHashCode();
            }
        }

        public readonly List<Member> Members = new List<Member>();
        private int lastOffset = 0;
        public override int ByteCount => lastOffset;


        public void AddMember(int id, Type type)
        {
            var offset = lastOffset;
            if (type.ByteCount >= Compiler.Instance.Alignment) {
                offset = Compiler.Instance.AlignedSize(offset);
            }
            Members.Add(new Member(type, id, offset));
            lastOffset = offset + type.ByteCount;
        }

        public override bool Equals(object? obj)
        {
            return obj is StructureType structType && (Members.Count == structType.Members.Count &&
                                                       !Members.Where((t, i) => !t.Equals(structType.Members[i])).Any());
        }

        public override int GetHashCode()
        {
            return Members.Sum(m => m.GetHashCode());
        }


        public override int Incremental => throw new System.NotImplementedException();
        public override int MaxElementSize => Members.Select(m => m.Type.MaxElementSize).Max();

        public override Constant DefaultValue()
        {
            var memberValues = Members.Select(member => member.Type.DefaultValue()).ToList();
            return new ConstantStructure(this, memberValues);
        }

        public override Constant? ParseConstant(Compiler compiler)
        {
            return compiler.ParseConstantStructure(this);
        }


        public Value MemberValue(Identifier identifier, AssignableValue value)
        {
            foreach (var member in Members) {
                if (member.Id != identifier.Id) continue;
                var memberType = member.Type;
                if (memberType is ArrayType arrayType) {
                    var structurePointer = value.Reference(identifier.Position);
                    var bytePointer = structurePointer!.CastTo(new PointerType(IntegerType.ByteType));
                    var addedBytePointer = bytePointer!.BinomialResult(identifier.Position, '+', new ConstantInteger(member.Offset));
                    return addedBytePointer!.CastTo(arrayType.ToPointerType())!;
                }
                var memberValue = new StructureMember(memberType, value, member.Offset);
                return memberValue; ;
            }
            throw new UndefinedIdentifierError(identifier);
        }
    }
}