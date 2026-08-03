namespace Inu.Cate.MuCom87.MuPD7800;

internal class Compiler() : MuCom87.Compiler(new ByteOperation(), new WordOperation())
{
    protected override BinomialInstruction CreateWordAddOrSubtractInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand)
    {
        return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
    }

    protected override MuCom87.ByteShiftInstruction CreateByteShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand leftOperand, Operand rightOperand)
    {
        return new ByteShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
    }

    public override void SkipIfZero(Instruction instruction)
    {
        instruction.WriteJumpLine("\tskz");
    }

    public override Cate.CompareInstruction CreateCompareInstruction(Function function, int operatorId, Operand leftOperand,
        Operand rightOperand, Anchor anchor)
    {
        return new CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor);
    }
}