namespace Inu.Cate
{
    public class NamedConstant : NamedValue
    {
        public readonly Value Value;

        public NamedConstant(Block block, int id, Type type, Value value) : base(block, id, type)
        {
            Value = value;
        }
    }
}