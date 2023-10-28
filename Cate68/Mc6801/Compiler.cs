using System;

namespace Inu.Cate.Mc6800.Mc6801;

internal class Compiler : Mc6800.Compiler
{
    public Compiler() : base(new ByteOperation(), new WordOperation(), new PointerOperation()) { }

    public override Register? ReturnRegister(ParameterizableType type)
    {
        return type.ByteCount switch
        {
            1 => ByteRegister.A,
            _ => type is PointerType ? Mc6801.PointerRegister.X : Mc6801.IndexRegister.X
        };
    }

    protected override LoadInstruction CreateWordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    {
        return new WordLoadInstruction(function, destinationOperand, sourceOperand);
    }

    protected override LoadInstruction CreatePointerLoadInstruction(Function function, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return new PointerLoadInstruction(function, destinationOperand, sourceOperand);
    }

    public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand leftOperand, Operand rightOperand)
    {
        if (destinationOperand.Type.ByteCount == 2) {
            switch (operatorId) {
                //case '|':
                //case '^':
                //case '&':
                //    return new WordBitInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case '+':
                case '-':
                    return new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
                case Keyword.ShiftLeft:
                case Keyword.ShiftRight:
                    return new WordShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
            }
        }

        return base.CreateBinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
    }

    //public override Cate.ResizeInstruction CreateResizeInstruction(Function function, AssignableOperand destinationOperand,
    //    IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
    //{
    //    return new ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType);
    //}

    public override Cate.MultiplyInstruction CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand,
        Operand leftOperand, int rightValue)
    {
        return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
    }
}