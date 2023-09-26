namespace Inu.Cate.Hd61700;

internal class SubroutineInstruction : Cate.SubroutineInstruction
{
    public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand, List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands) { }

    protected override void Call()
    {
        WriteLine("\tcal " + TargetFunction.Label);
    }

    protected override void StoreParameters()
    {
        StoreParametersViaPointer();
    }
}