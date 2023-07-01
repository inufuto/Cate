using System.Collections.Generic;

namespace Inu.Cate.Mc6809
{
    internal class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand, List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands)
        { }

        public static Register? ParameterRegister(in int index, in ParameterizableType type)
        {
            return index switch
            {
                0 => type.ByteCount == 1 ? ByteRegister.A : type is PointerType ? PointerRegister.X : WordRegister.X,
                1 => type.ByteCount == 1 ? ByteRegister.B : type is PointerType ? PointerRegister.Y : WordRegister.Y,
                _ => null
            };
        }


        protected override void Call()
        {
            if (TargetFunction.Visibility == Visibility.External) {
                WriteLine("\tjsr\t" + TargetFunction.Label);
            }
            else {
                WriteLine("\tbsr\t" + TargetFunction.Label);
            }
        }

        protected override void StoreParameters()
        {
            StoreParametersDirect();
        }

        public static Register? ReturnRegister(in ParameterizableType type)
        {
            return type.ByteCount switch
            {
                1 => ByteRegister.A,
                2 => type is PointerType ? PointerRegister.D : WordRegister.D,
                _ => null
            };
        }


    }
}