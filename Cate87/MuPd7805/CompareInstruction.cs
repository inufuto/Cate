using System;

namespace Inu.Cate.MuCom87.MuPd7805
{
    internal class CompareInstruction : MuCom87.CompareInstruction
    {
        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand, Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor)
        { }

        protected override void OperateViaAccumulator(string operation, Action action)
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                using (var reservation = ByteOperation.ReserveAnyRegister(this, ByteRegister.RegistersOtherThan(ByteRegister.A))) {
                    var temporaryRegister = reservation.ByteRegister;
                    ByteRegister.A.Load(this, RightOperand);
                    temporaryRegister.CopyFrom(this, ByteRegister.A);
                    ByteRegister.A.Load(this, LeftOperand);
                    WriteLine("\t" + operation.Split('|')[0] + "\ta," + temporaryRegister);
                    WriteLine("\tmvi\ta,1");
                    WriteLine("\tmvi\ta,0");
                    AddChanged(ByteRegister.A);
                    RemoveRegisterAssignment(ByteRegister.A);
                    WriteJumpLine("\tdcr\ta");
                    action();
                }
            }
        }
    }
}
