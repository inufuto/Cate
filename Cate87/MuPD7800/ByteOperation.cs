namespace Inu.Cate.MuCom87.MuPD7800;

internal class ByteOperation : MuCom87.ByteOperation
{
    public override void StoreConstantIndirect(Instruction instruction, Cate.WordRegister pointerRegister,
        int offset, int value)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmvix\t" + pointerRegister.AsmName + "," + value);
            return;
        }
        base.StoreConstantIndirect(instruction, pointerRegister, offset, value);
    }

    public override void OperateByteBinomial(BinomialInstruction instruction, string operation, bool change)
    {
        if (instruction.RightOperand is ConstantOperand constantOperand) {
            if (instruction.DestinationOperand.Register is ByteRegister operandRegister) {
                ViaRegister(operandRegister);
                return;
            }

            using var reservation = ByteOperation.ReserveAnyRegister(instruction);
            ViaRegister(reservation.ByteRegister);
            return;
            
            void ViaRegister(Cate.ByteRegister byteRegister)
            {
                byteRegister.Load(instruction, instruction.LeftOperand);
                instruction.WriteLine("\t" + operation.Split("|")[1] + "\t" + byteRegister.AsmName + "," + constantOperand);
                byteRegister.Store(instruction, instruction.DestinationOperand);
            }
        }
        base.OperateByteBinomial(instruction, operation, change);
    }

    public override void OperateByteRegister(Instruction instruction, ByteRegister byteRegister, string operation, bool change,
        ConstantOperand constantOperand)
    {
        instruction.WriteLine("\t" + operation.Split("|")[1] + "\t" + byteRegister.AsmName + "," + constantOperand);
    }
}