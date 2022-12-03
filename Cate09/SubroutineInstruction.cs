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
                0 => type.ByteCount == 1 ? (Register)ByteRegister.A : WordRegister.X,
                1 => type.ByteCount == 1 ? (Register)ByteRegister.B : WordRegister.Y,
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

        public static Register? ReturnRegister(in int byteCount)
        {
            return byteCount switch
            {
                1 => ByteRegister.A,
                2 => WordRegister.D,
                _ => null
            };
        }


    }
}