using System.Diagnostics;

namespace Inu.Cate.MuCom87.MuPD7800;

internal class WordAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : MuCom87.WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{

    protected override void BuildAssembly(string lowOperation, string highOperation)
    {
        if (RightOperand is ConstantOperand constantOperand)
        {
            if (DestinationOperand.Register is WordRegister operandRegister)
            {
                ViaRegister(operandRegister);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
            ViaRegister(reservation.WordRegister);
            return;

            void ViaRegister(Cate.WordRegister wordRegister)
            {
                wordRegister.Load(this, LeftOperand);
                Debug.Assert(wordRegister.Low != null);
                Debug.Assert(wordRegister.High != null);
                wordRegister.Low.Operate(this, lowOperation, true, Compiler.LowByteOperand(constantOperand));
                wordRegister.High.Operate(this, highOperation, true, Compiler.HighByteOperand(constantOperand));
                RemoveRegisterAssignment(wordRegister);
                AddChanged(wordRegister);
                wordRegister.Store(this, DestinationOperand);
            }
        }
        base.BuildAssembly(lowOperation, highOperation);
    }
}