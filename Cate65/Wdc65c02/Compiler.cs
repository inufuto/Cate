using Inu.Cate.Mos6502;

namespace Inu.Cate.Wdc65c02;

internal class Compiler : Mos6502.Compiler
{
    public Compiler(bool parameterRegister) : base(parameterRegister) { }

    public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand leftOperand, Operand rightOperand)
    {
        if (destinationOperand.Type.ByteCount == 1 && operatorId is '+' or '-') {
            return new ByteAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
        }
        return base.CreateBinomialInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand);
    }

    public override void LoadIndirect(Instruction instruction, Mos6502.ByteRegister byteRegister, WordZeroPage zeroPage, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tld" + byteRegister.AsmName + "\t(" + zeroPage.Name + ")");
            return;
        }
        base.LoadIndirect(instruction, byteRegister, zeroPage, offset);
    }

    public override void StoreIndirect(Instruction instruction, Mos6502.ByteRegister byteRegister, WordZeroPage zeroPage, int offset)
    {
        if (Equals(byteRegister, Mos6502.ByteRegister.A) && offset == 0) {
            instruction.WriteLine("\tst" + byteRegister.AsmName + "\t(" + zeroPage.AsmName + ")");
            return;
        }
        base.StoreIndirect(instruction, byteRegister, zeroPage, offset);
    }

    public override void OperateIndirect(Instruction instruction, string operation, WordZeroPage zeroPage, int offset, int count)
    {
        if (offset == 0) {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t(" + zeroPage.Name + ")");
            }
            return;
        }
        base.OperateIndirect(instruction, operation, zeroPage, offset, count);
    }

    public override void ClearByte(Instruction instruction, string label)
    {
        instruction.WriteLine("\tstz\t" + label);
    }
}