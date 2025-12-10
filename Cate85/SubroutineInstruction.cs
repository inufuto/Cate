namespace Inu.Cate.Sm85;

internal class SubroutineInstruction(
    Function function,
    Function targetFunction,
    AssignableOperand? destinationOperand,
    List<Operand> sourceOperands)
    : Cate.SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands)
{
    protected override void Call()
    {
        WriteLine("\tcall\t" + TargetFunction.Label);
    }

    protected override void StoreParameters()
    {
        StoreParametersViaPointer();
    }
    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (register.Conflicts(WordRegister.FromAddress(0)) && DestinationOperand is IndirectOperand indirectOperand && indirectOperand.Variable == variable) {
            return null;
        }
        return base.RegisterAdaptability(variable, register);
    }
}