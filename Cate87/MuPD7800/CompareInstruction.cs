using System;

namespace Inu.Cate.MuCom87.MuPD7800
{
    internal class CompareInstruction : MuCom87.CompareInstruction
    {
        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor)
        { }

        protected override void OperateViaAccumulator(string operation, Action action)
        {
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteRegister.A.Load(this, RightOperand);
                WriteLine("\tstaw\t" + MuCom87.Compiler.TemporaryByte);
            });
            OperateWorkingRegister(operation, action, MuCom87.Compiler.TemporaryByte);
        }

        private void OperateWorkingRegister(string operation, Action action, string name)
        {
            ByteRegister.A.Load(this, LeftOperand);
            WriteJumpLine("\t" + operation.Split('|')[0] + "w\t" + name);
            action();
        }
    }
}
