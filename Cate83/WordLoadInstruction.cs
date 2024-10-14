namespace Inu.Cate.Sm83;

internal class WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    : Cate.WordLoadInstruction(function, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        if (DestinationOperand is VariableOperand { Register: null } variableOperand) {
            if (!IsRegisterReserved(PointerRegister.Hl)) {
                if (SourceOperand is IntegerOperand integerOperand) {
                    using (PointerOperation.ReserveRegister(this, PointerRegister.Hl)) {
                        PointerRegister.Hl.LoadConstant(this, variableOperand.MemoryAddress());
                        WordOperation.StoreConstantIndirect(this, PointerRegister.Hl, variableOperand.Offset,
                            integerOperand.IntegerValue);
                    }
                    return;
                }
            }
            else {
                using var reservation = ByteOperation.ReserveRegister(this, ByteRegister.A);
                ByteRegister.A.Load(this, Compiler.LowByteOperand(SourceOperand));
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                ByteRegister.A.Load(this, Compiler.HighByteOperand(SourceOperand));
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                return;
            }
        }
        base.BuildAssembly();
    }
}