namespace Inu.Cate.Hd61700;

internal class ByteMonomialInstruction : Cate.MonomialInstruction
{
    public ByteMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

    public override void BuildAssembly()
    {
        Action<Cate.ByteRegister> operation = OperatorId switch
        {
            '-' => r =>
            {
                WriteLine("\tcmp " + r.AsmName);
            }
            ,
            '~' => r =>
            {
                WriteLine("\tinv " + r.AsmName);
            }
            ,
            _ => throw new NotImplementedException()
        };

        void ViaRegister(Cate.ByteRegister r)
        {
            r.Load(this, SourceOperand);
            operation(r);
            r.Store(this, DestinationOperand);
        }

        if (DestinationOperand.Register is ByteRegister destinationOperandRegister) {
            ViaRegister(destinationOperandRegister);
            return;
        }

        using var reservation = ByteOperation.ReserveAnyRegister(this,  SourceOperand);
        ViaRegister(reservation.ByteRegister);
    }
}