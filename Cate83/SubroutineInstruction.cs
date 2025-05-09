﻿namespace Inu.Cate.Sm83;

internal class SubroutineInstruction(
    Function function,
    Function targetFunction,
    AssignableOperand? destinationOperand,
    List<Operand> sourceOperands)
    : Cate.SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands)
{
    public static Register? ReturnRegister(ParameterizableType type)
    {
        return type.ByteCount switch
        {
            1 => ByteRegister.A,
            2 => WordRegister.Hl,
            _ => null
        };
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

    protected override void Call()
    {
        WriteLine("\tcall\t" + TargetFunction.Label);
    }

    protected override void StoreParameters()
    {
        if (IsRegisterReserved(WordRegister.Hl)) {
            StoreParametersDirect();
        }
        else {
            StoreParametersViaPointer();
        }
    }
}