namespace Inu.Cate.Wdc65816;

internal class MonomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand sourceOperand)
    : Cate.MonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        if (DestinationOperand.Type.ByteCount == 1) {
            void ViaA()
            {
                ByteRegister.A.Load(this, SourceOperand);
                ByteRegister.A.Operate(this, "eor", true, "#$ff");
                if (OperatorId == '-') {
                    ByteRegister.A.Operate(this, "clc|adc", true, "#1");
                }
                ByteRegister.A.Store(this, DestinationOperand);
            }

            if (Equals(DestinationOperand.Register, ByteRegister.A)) {
                ViaA();
            }
            else {
                using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                    ViaA();
                }
            }
        }
        else
        {
            void ViaA()
            {
                WordRegister.A.Load(this, SourceOperand);
                WordRegister.A.Operate(this, "eor", true, "#$ff");
                if (OperatorId == '-') {
                    WordRegister.A.Operate(this, "clc|adc", true, "#1");
                }
                WordRegister.A.Store(this, DestinationOperand);
            }

            if (Equals(DestinationOperand.Register, WordRegister.A)) {
                ViaA();
            }
            else {
                using (WordOperation.ReserveRegister(this, WordRegister.A)) {
                    ViaA();
                }
            }
        }
    }
}