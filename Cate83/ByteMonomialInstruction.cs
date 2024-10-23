namespace Inu.Cate.Sm83;

internal class ByteMonomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand sourceOperand)
    : MonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        if (Equals(SourceOperand.Register, ByteRegister.A)) {
            Operate();
            ByteRegister.A.Store(this, DestinationOperand);
            return;
        }
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            ByteRegister.A.Load(this, SourceOperand);
            Operate();
            ByteRegister.A.Store(this, DestinationOperand);
        }

        return;

        void Operate()
        {
            WriteLine("\tcpl");
            if (OperatorId == '-') {
                WriteLine("\tinc\ta");
            }
        }
    }
}