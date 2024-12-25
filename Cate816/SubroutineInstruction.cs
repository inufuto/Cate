using System.Diagnostics;

namespace Inu.Cate.Wdc65816;

internal class SubroutineInstruction(
    Function function,
    Function targetFunction,
    AssignableOperand? destinationOperand,
    List<Operand> sourceOperands)
    : Cate.SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands)
{
    protected override Register? ResolveReturnRegister(List<RegisterReservation> reservations, Register? returnRegister, ref RegisterReservation? alternative)
    {
        return returnRegister;
    }

    protected override void Call()
    {
        if (TargetFunction.Parameters.Count > 0) {
            var parameterRegister = TargetFunction.Parameters[0].Register;
            Debug.Assert(parameterRegister != null);
            Wdc65816.Compiler.MakeSize(parameterRegister, this);
        }
        WriteLine("\tjsr\t" + TargetFunction.Label);
        var returnRegister = Compiler.ReturnRegister((ParameterizableType)TargetFunction.Type);
        switch (returnRegister) {
            case ByteRegister byteRegister:
                byteRegister.MakeSize(this);
                break;
            case WordRegister wordRegister:
                wordRegister.MakeSize(this);
                break;
        }
    }

    protected override void StoreParameters()
    {
        StoreParametersDirect();
    }

    protected override List<Cate.ByteRegister> Candidates(Operand operand)
    {
        return [ByteRegister.A,];
    }

    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (register.Equals(ByteRegister.A) || register.Equals(WordRegister.A)) {
            {
                if (DestinationOperand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)) {
                    return null;
                }
            }
            if (SourceOperands.Count > 1) {
                foreach (var operand in SourceOperands) {
                    if (operand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)) {
                        return null;
                    }
                }
            }
        }
        return base.RegisterAdaptability(variable, register);
    }
}