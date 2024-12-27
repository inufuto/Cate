using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate;

internal class FunctionCall(Function function) : Value(function.Type)
{
    public readonly Function Function = function;
    public readonly List<Value> Parameters = [];

    public void AddParameter(Value value)
    {
        Parameters.Add(value);
    }

    public override void BuildInstructions(Function function, AssignableOperand destinationOperand)
    {
        var compiler = Compiler.Instance;
        var operands = Parameters.Select(p => p.ToOperand(function)).ToList();
        var instruction = compiler.CreateSubroutineInstruction(function, Function, destinationOperand, operands);
        function.Instructions.Add(instruction);
    }

    public override void BuildInstructions(Function function)
    {
        var compiler = Compiler.Instance;
        var operands = Parameters.Select(p => p.ToOperand(function)).ToList();
        var instruction = compiler.CreateSubroutineInstruction(function, Function, null, operands);
        function.Instructions.Add(instruction);
    }
}