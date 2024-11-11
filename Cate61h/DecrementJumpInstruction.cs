namespace Inu.Cate.Hd61700;

internal class DecrementJumpInstruction : Cate.DecrementJumpInstruction
{
    public DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor) : base(function, operand, anchor) { }

    public override void BuildAssembly()
    {
        switch (Operand) {
            case VariableOperand when Operand.Register is ByteRegister register:
                WriteLine("\tsb " + register.AsmName + "," + ByteRegister.IntValue(1));
                break;
            case VariableOperand variableOperand: {
                using var reservation = WordOperation.ReserveAnyRegister(this, IndexRegister.Registers(true), Operand);
                var pointerRegister = reservation.WordRegister;
                pointerRegister.LoadConstant(this, variableOperand.MemoryAddress());
                WriteLine("\tsb (" + pointerRegister.AsmName + IndexRegister.OffsetValue(0) + "),$30");
                break;
            }
            case IndirectOperand indirectOperand: {
                using var reservation = ByteOperation.ReserveAnyRegister(this);
                var byteRegister = reservation.ByteRegister;
                byteRegister.LoadIndirect(this, indirectOperand.Variable, indirectOperand.Offset);
                WriteLine("\tsb " + byteRegister.AsmName + "," + IndexRegister.OffsetValue(1));
                byteRegister.StoreIndirect(this, indirectOperand.Variable, indirectOperand.Offset);
                break;
            }
            default:
                throw new NotImplementedException();
        }
        WriteJumpLine("\tjr nz," + Anchor.Label);
    }
}