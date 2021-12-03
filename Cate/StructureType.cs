using System.Collections.Generic;
using System.Linq;
using Inu.Language;

namespace Inu.Cate
{
    public class StructureType : Type
    {
        public class Member
        {
            public readonly Type Type;
            public readonly int Id;

            public Member(Type type, int id)
            {
                Id = id;
                Type = type;
            }

            public override bool Equals(object? obj)
            {
                return obj is Member member && (Id == member.Id && Type.Equals(member.Type));
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() + Type.GetHashCode();
            }
        }

        public readonly List<Member> Members = new List<Member>();

        public void AddMember(int id, Type type) { Members.Add(new Member(type, id)); }

        public override bool Equals(object? obj)
        {
            return obj is StructureType structType && (Members.Count == structType.Members.Count &&
                                                       !Members.Where((t, i) => !t.Equals(structType.Members[i])).Any());
        }

        public override int GetHashCode()
        {
            return Members.Sum(m => m.GetHashCode());
        }

        public override int ByteCount => Members.Sum(m => m.Type.ByteCount);
        public override int Incremental => throw new System.NotImplementedException();

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
            var offset = 0;
            foreach (var member in Members) {
                if (member.Id == identifier.Id) {
                    var memberType = member.Type;
                    if (memberType is ArrayType arrayType) {
                        var structurePointer = value.Reference(identifier.Position);
                        var bytePointer = structurePointer!.CastTo(new PointerType(IntegerType.ByteType));
                        var addedBytePointer = bytePointer!.BinomialResult(identifier.Position, '+', new ConstantInteger(offset));
                        return addedBytePointer!.CastTo(arrayType.ToPointerType())!;
                    }
                    var memberValue = new StructureMember(memberType, value, offset);
                    return memberValue; ;
                }
                offset += member.Type.ByteCount;
            }
            throw new UndefinedIdentifierError(identifier);
        }
    }
}