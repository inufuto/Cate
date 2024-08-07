namespace Inu.Cate.Mos6502;

//internal abstract class ParameterAssignment
//{
//    public abstract Register? ParameterRegister(int index, ParameterizableType type);

//    public Register? ReturnRegister(ParameterizableType type)
//    {
//        return type.ByteCount switch
//        {
//            1 => ByteRegister.Y,
//            2 => (type is PointerType ? PairPointerRegister.Xy : PairWordRegister.Xy),
//            _ => null
//        };
//    }
//}

//internal class ParameterAssignment1 : ParameterAssignment
//{
//    public override Register? ParameterRegister(int index, ParameterizableType type)
//    {
//        return null;
//    }
//}

//internal class ParameterAssignment2 : ParameterAssignment
//{
//    public override Register? ParameterRegister(int index, ParameterizableType type)
//    {
//        if (index >= WordZeroPage.Count) return null;
//        switch (type.ByteCount) {
//            case 1:
//                return ByteZeroPage.FromOffset(index * 2);
//            case 2:
//                var wordRegister = WordZeroPage.FromOffset(index);
//                if (type is PointerType && wordRegister != null)
//                    return wordRegister.ToPointer();
//                return wordRegister;
//        }
//        return null;
//    }
//}
