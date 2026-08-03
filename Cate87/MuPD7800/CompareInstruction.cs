using System;

namespace Inu.Cate.MuCom87.MuPD7800;

internal class CompareInstruction(
    Function function,
    int operatorId,
    Operand leftOperand,
    Operand rightOperand,
    Anchor anchor)
    : MuCom87.CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor)
{
    protected override void OperateViaAccumulator(string operation, Action action)
    {
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            ByteRegister.A.Load(this, RightOperand);
            WriteLine("\tstaw\t" + MuCom87.Compiler.TemporaryByte);
        }
        OperateWorkingRegister(operation, action, MuCom87.Compiler.TemporaryByte);
    }

    private void OperateWorkingRegister(string operation, Action action, string name)
    {
        ByteRegister.A.Load(this, LeftOperand);
        WriteJumpLine("\t" + operation.Split('|')[0] + "w\t" + name);
        action();
    }

    protected override void OperateConstant(string operation, Action action, string value)
    {
        if (LeftOperand.Register is ByteRegister byteRegister) {
            byteRegister.Load(this, LeftOperand);
            WriteJumpLine("\t" + operation.Split('|')[1] + "\t" + byteRegister.AsmName + "," + value);
            action();
            return;
        }
        base.OperateConstant(operation, action, value);
    }
}