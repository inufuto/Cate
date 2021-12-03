using System;

namespace Inu.Cate
{
    class Reference : Value
    {
        private readonly AssignableValue Value;

        public Reference(AssignableValue value) : base(new PointerType(value.Type))
        {
            Value = value;
            if (value is VariableValue variableValue) {
                variableValue.Variable.Static = true;
            }
        }

        private new PointerType Type => (PointerType)base.Type;
        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            Operand sourceOperand;
            switch (Value) {
                case VariableValue variableValue:
                    sourceOperand = new PointerOperand(variableValue.Type, variableValue.Variable, 0);
                    break;
                case Dereference dereference:
                    sourceOperand = dereference.ToOperand(function);
                    break;
                case StructureMember structureMember:
                    sourceOperand = structureMember.ToAssignableOperand(function);
                    break;
                default:
                    throw new NotImplementedException();
            }

            var instruction = Compiler.Instance.CreateLoadInstruction(function, destinationOperand, sourceOperand);
            function.Instructions.Add(instruction);
        }

        public override void BuildInstructions(Function function)
        {
            switch (Value) {
                //case VariableValue variableValue:
                //    sourceOperand = new PointerOperand(variableValue.Type, variableValue.Variable, 0);
                //    break;
                case Dereference dereference:
                    dereference.BuildInstructions(function);
                    break;
                case StructureMember structureMember:
                    structureMember.BuildInstructions(function);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}