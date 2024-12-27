namespace Inu.Cate.Wdc65816;

internal class DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
    : Cate.DecrementJumpInstruction(function, operand, anchor)
{
    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (Equals(register, ByteRegister.A) && Operand is VariableOperand variableOperand && variableOperand.Variable.Equals(variable)) {
            return null;
        }
        return base.RegisterAdaptability(variable, register);
    }

    public override void BuildAssembly()
    {
        if (Operand.Register is ByteRegister register) {
            register.Decrement(this);
        }
        else {
            ByteOperation.Operate(this, "dec", true, Operand);
        }
        WriteLine("\tbne\t" + Anchor.Label);
    }
}