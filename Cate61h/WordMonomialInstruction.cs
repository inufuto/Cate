namespace Inu.Cate.Hd61700;

internal class WordMonomialInstruction:Cate.MonomialInstruction
{
    public WordMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand) { }

    public override void BuildAssembly()
    {
        Action<Cate.WordRegister> operation = OperatorId switch
        {
            '-' => r =>
            {
                WriteLine("\tcmpw " + r.AsmName);
            }
            ,
            '~' => r =>
            {
                WriteLine("\tinvw " + r.AsmName);
            }
            ,
            _ => throw new NotImplementedException()
        };

        void ViaRegister(Cate.WordRegister r)
        {
            r.Load(this, SourceOperand);
            operation(r);
            r.Store(this, DestinationOperand);
        }

        if (DestinationOperand.Register is WordRegister destinationOperandRegister) {
            ViaRegister(destinationOperandRegister);
            return;
        }

        using var reservation = WordOperation.ReserveAnyRegister(this, SourceOperand);
        ViaRegister(reservation.WordRegister);
    }
}