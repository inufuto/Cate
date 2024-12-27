namespace Inu.Cate.Hd61700;

internal class DecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
    : Cate.DecrementJumpInstruction(function, operand, anchor)
{
    public override void BuildAssembly()
    {
        switch (Operand) {
            case VariableOperand when Operand.Register is ByteRegister register:
                WriteLine("\tsb " + register.AsmName + "," + ByteRegister.IntValue(1));
                break;
            case VariableOperand variableOperand: {
                var indexRegister = IndexRegister.Ix;
                indexRegister.LoadConstant(this, variableOperand.MemoryAddress());
                WriteLine("\tsb (" + indexRegister.AsmName + IndexRegister.OffsetValue(0) + "),$30");
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