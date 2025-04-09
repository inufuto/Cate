namespace Inu.Cate.Tlcs900;

internal class WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) :Cate.WordLoadInstruction(function, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        if (
            DestinationOperand is VariableOperand { Register: null } destinationVariableOperand
        ) {
            switch (SourceOperand) {
                case IntegerOperand integerOperand:
                    WriteLine("\tldw (" + destinationVariableOperand.MemoryAddress() + ")," + integerOperand.IntegerValue);
                    return;
                case PointerOperand pointerOperand:
                    WriteLine("\tldw (" + destinationVariableOperand.MemoryAddress() + ")," + pointerOperand.MemoryAddress());
                    return;
            }
        }
        base.BuildAssembly();
    }
}