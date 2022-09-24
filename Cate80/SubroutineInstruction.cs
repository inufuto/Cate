using System.Collections.Generic;

namespace Inu.Cate.Z80
{
    class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand,
            List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands)
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
                0 => type.ByteCount switch
                {
                    1 => ByteRegister.A,
                    _ => type switch
                    {
                        PointerType { ElementType: StructureType _ } => WordRegister.Ix,
                        _ => WordRegister.Hl
                    }
                },
                1 => type.ByteCount switch
                {
                    1 => ByteRegister.E,
                    _ => type switch
                    {
                        PointerType { ElementType: StructureType _ } => WordRegister.Iy,
                        _ => WordRegister.De
                    }
                },
                2 => type.ByteCount switch
                {
                    1 => ByteRegister.C,
                    _ => WordRegister.Bc
                },
                _ => null
            };
        }

        public static Register? ReturnRegister(int byteCount)
        {
            return byteCount switch
            {
                1 => ByteRegister.A,
                2 => WordRegister.Hl,
                _=>null
            };
        }
    }
}