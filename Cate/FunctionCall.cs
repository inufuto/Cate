using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate
{
    internal class FunctionCall : Value
    {
        public readonly Function Function;
        public readonly List<Value> Parameters = new List<Value>();

        public FunctionCall(Function function) : base(function.Type)
        {
            Function = function;
        }

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
}
