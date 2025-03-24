namespace Inu.Cate.Tlcs900;

internal class ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) :Cate.ByteLoadInstruction(function, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        if (
            DestinationOperand is VariableOperand { Register: null } destinationVariableOperand
        ) {
            if (SourceOperand is IntegerOperand integerOperand) {
                WriteLine("\tld (" + destinationVariableOperand.MemoryAddress() + ")," + integerOperand.IntegerValue);
                return;
            }
        }
        base.BuildAssembly();
    }
}