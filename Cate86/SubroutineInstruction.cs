﻿using System.Collections.Generic;

namespace Inu.Cate.I8086
{
    internal class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand, List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands)
        {
        }

        protected override void Call()
        {
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
                0 => type.ByteCount switch
                {
                    1 => ByteRegister.Al,
                    _ => type switch
                    {
                        PointerType { ElementType: StructureType _ } => PointerRegister.Bx,
                        PointerType=>PointerRegister.Ax,
                        _ => WordRegister.Ax
                    }
                },
                1 => type.ByteCount switch
                {
                    1 => ByteRegister.Dl,
                    _ => type switch
                    {
                        PointerType { ElementType: StructureType _ } => PointerRegister.Si,
                        PointerType => PointerRegister.Dx,
                        _ => WordRegister.Dx
                    }
                },
                2 => type.ByteCount switch
                {
                    1 => ByteRegister.Cl,
                    _ => type switch
                    {
                        PointerType { ElementType: StructureType _ } => PointerRegister.Di,
                        PointerType => PointerRegister.Dx,
                        _ => WordRegister.Cx
                    }
                },
                _ => null
            };
        }

        public static Register? ReturnRegister(ParameterizableType type)
        {
            return type.ByteCount switch
            {
                1 => ByteRegister.Al,
                2 => type is PointerType ? PointerRegister.Ax : WordRegister.Ax,
                _ => null
            };
        }
    }
}
