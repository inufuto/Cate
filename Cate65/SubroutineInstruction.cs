using System.Collections.Generic;

namespace Inu.Cate.Mos6502;

internal class SubroutineInstruction(
    Function function,
    Function targetFunction,
    AssignableOperand? destinationOperand,
    List<Operand> sourceOperands)
    : Cate.SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands)
{
    protected override void Call()
    {
        WriteLine("\tjsr\t" + TargetFunction.Label);
        RemoveRegisterAssignment(ByteRegister.X);
        RemoveRegisterAssignment(ByteRegister.Y);
    }

    protected override void StoreParameters()
    {
        StoreParametersDirect();
    }

    protected override List<Cate.ByteRegister> Candidates(Operand operand)
    {
        return operand switch
        {
            IndirectOperand _ => [ByteRegister.A],
            _ => base.Candidates(operand)
        };
    }
}