using System.Collections.Generic;

namespace Inu.Cate.MuCom87
{
    class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand, List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands)
        { }

        protected override void Call()
        {
            WriteLine("\tcall\t" + TargetFunction.Label);
        }

        protected override void StoreParameters()
        {
            StoreParametersViaPointer();
        }

        public static Register? ParameterRegister(int index, ParameterizableType type)
        {
            return index switch
            {
                0 when type.ByteCount == 1 => ByteRegister.A,
                0 => WordRegister.Hl,
                1 when type.ByteCount == 1 => ByteRegister.E,
                1 => WordRegister.De,
                2 when type.ByteCount == 1 => ByteRegister.C,
                2 => WordRegister.Bc,
                _ => null
            };
        }

        public static Register ReturnRegister(int byteCount)
        {
            return byteCount switch
            {
                1 => ByteRegister.A,
                _ => WordRegister.Hl
            };
        }
    }
}
