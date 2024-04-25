namespace Inu.Cate.Wdc65c02;

internal class ByteAddOrSubtractInstruction : Mos6502.ByteAddOrSubtractInstruction
{
    public ByteAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

    protected override bool CanIncrementOrDecrement() => true;
    protected override int Threshold() => 2;

    protected override bool IsOperatable(AssignableOperand operand)
    {
        return operand is not IndirectOperand && base.IsOperatable(operand);
    }
}