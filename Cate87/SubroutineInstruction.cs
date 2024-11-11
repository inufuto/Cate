using System.Collections.Generic;

namespace Inu.Cate.MuCom87
{
    internal class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public const string TemporaryByte = MuCom87.Compiler.TemporaryByte;   //"@TempParam";
        //private bool accumulatorSaved=false;

        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand,
            List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands)
        {
            ParameterAssignments.Reverse();
        }

        protected override void Call()
        {
            //if (accumulatorSaved) {
            //    WriteLine("\tldaw\t" + TemporaryByte);
            //}
            WriteLine("\tcall\t" + TargetFunction.Label);
        }


        protected override void StoreParameters()
        {
            StoreParametersDirect();
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

        public static Register? ReturnRegister(ParameterizableType type)
        {
            return type.ByteCount switch
            {
                1 => ByteRegister.A,
                2 => WordRegister.Hl,
                _ => null
            };
        }
    }
}
