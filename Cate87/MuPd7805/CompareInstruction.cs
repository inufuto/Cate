using System;
using System.Xml.Linq;

namespace Inu.Cate.MuCom87.MuPd7805
{
    internal class CompareInstruction : MuCom87.CompareInstruction
    {
        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor)
        { }

        protected override void OperateViaAccumulator(string operation, Action action)
        {
            ByteOperation.UsingRegister(this, ByteRegister.A, () =>
            {
                ByteOperation.UsingAnyRegister(this, ByteRegister.RegistersOtherThan(ByteRegister.A),
                    temporaryRegister =>
                {
                    ByteRegister.A.Load(this, RightOperand);
                    temporaryRegister.CopyFrom(this, ByteRegister.A);
                    ByteRegister.A.Load(this, LeftOperand);
                    WriteLine("\t" + operation.Split('|')[0] + "\ta," + temporaryRegister);
                    WriteLine("\tmvi\ta,1");
                    WriteLine("\tmvi\ta,0");
                    ChangedRegisters.Add(ByteRegister.A);
                    RemoveRegisterAssignment(ByteRegister.A);
                    WriteJumpLine("\tdcr\ta");
                    action();
                });
            });
        }
    }
}
