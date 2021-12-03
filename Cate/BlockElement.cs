using Inu.Language;

namespace Inu.Cate
{
    public abstract class BlockElement
    {
        public readonly Block Block;

        protected BlockElement(Block block)
        {
            Block = block;
        }
    }

    public abstract class NamedElement : BlockElement
    {
        public readonly int Id;
#if DEBUG
        public readonly string Name;
#else
        public string Name => Identifier.FromId(Id)!;
#endif

        protected NamedElement(Block block, int id) : base(block)
        {
            Id = id;
#if DEBUG
            Name = Identifier.FromId(Id)!;
#endif
        }
    }

    public class NamedType : NamedElement
    {
        public readonly Type Type;

        public NamedType(Block block, int id, Type type) : base(block, id)
        {
            Type = type;
        }
    }

    public abstract class NamedValue : NamedElement
    {
        public readonly Type Type;

        protected NamedValue(Block block, int id, Type type) : base(block, id)
        {
            Type = type;
        }

        public virtual string Label => Block.LabelPrefix + Name + "_";
    }

    class Label : NamedElement
    {
        public readonly int Address;

        public Label(Block block, int pass, int id, int address) : base(block, id)
        {
            Address = address;
        }
    }
}
