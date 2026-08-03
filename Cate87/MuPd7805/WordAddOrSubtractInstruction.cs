namespace Inu.Cate.MuCom87.MuPd7805;

internal class WordAddOrSubtractInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : MuCom87.WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);