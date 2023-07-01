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
                        PointerType { ElementType: StructureType _ } => PointerRegister.Ix,
                        PointerType => PointerRegister.Hl,
                        _ => WordRegister.Hl
                    }
                },
                1 => type.ByteCount switch
                {
                    1 => ByteRegister.E,
                    _ => type switch
                    {
                        PointerType { ElementType: StructureType _ } => PointerRegister.Iy,
                        PointerType => PointerRegister.De,
                        _ => WordRegister.De
                    }
                },
                2 => type.ByteCount switch
                {
                    1 => ByteRegister.C,
                    _ => type switch
                    {
                        PointerType => PointerRegister.Bc,
                        _ => WordRegister.Bc
                    }
                },
                _ => null
            };
        }

        public static Register? ReturnRegister(ParameterizableType type)
        {
            return type.ByteCount switch
            {
                1 => ByteRegister.A,
                2 => type is PointerType ? PointerRegister.Hl : WordRegister.Hl,
                _ => null
            };
        }
    }
}