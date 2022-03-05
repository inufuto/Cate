namespace Inu.Cate
{
    public abstract class ParameterizableType : Type
    {
        public override int MaxElementSize => ByteCount;

        public override Value? Cast(Value value, Type type)
        {
            if (type is ParameterizableType parameterizableType) {
                if (ByteCount == parameterizableType.ByteCount) {
                    return new TypeChange(parameterizableType, value);
                }
            }
            return base.Cast(value, type);
        }
    }
}