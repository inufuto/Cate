namespace Inu.Cate.Tlcs900;

internal class SubroutineInstruction(
    Function function,
    Function targetFunction,
    AssignableOperand? destinationOperand,
    List<Operand> sourceOperands)
    : Cate.SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands)
{
    protected override void Call()
    {
        WriteLine("\tcall " + TargetFunction.Label);
    }

    protected override void StoreParameters()
    {
        StoreParametersDirect();
    }
}