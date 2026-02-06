namespace Inu.Cate;

public class Assignment(AssignableValue leftValue, Value rightValue) : Value(leftValue.Type)
{
    public readonly Value RightValue = rightValue;

    public override void BuildInstructions(Function function,
        AssignableOperand destinationOperand)
    {
        var leftOperand = leftValue.ToAssignableOperand(function);
        var rightOperand = RightValue.ToOperand(function);
        function.Instructions.Add(Compiler.CreateLoadInstruction(function, leftOperand, rightOperand));
        function.Instructions.Add(Compiler.CreateLoadInstruction(function, destinationOperand, leftOperand));
    }

    public override void BuildInstructions(Function function)
    {
        var destinationOperand = leftValue.ToAssignableOperand(function);
        Compiler.BuildAssignmentInstructions(this, function, destinationOperand);
    }

    public override Operand ToOperand(Function function)
    {
        switch (leftValue) {
            case VariableValue variableValue: {
                    RightValue.BuildInstructions(function, variableValue.ToAssignableOperand(function));
                    return variableValue.ToOperand(function);
                }
            default: {
                    var variable = function.CreateTemporaryVariable(Type);
                    RightValue.BuildInstructions(function, variable.ToAssignableOperand());
                    var leftOperand = leftValue.ToAssignableOperand(function);
                    var temporaryOperand = variable.ToOperand();
                    function.Instructions.Add(Compiler.CreateLoadInstruction(function, leftOperand, temporaryOperand));
                    return temporaryOperand;
                }
        }
    }
}