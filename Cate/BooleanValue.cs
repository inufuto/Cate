namespace Inu.Cate
{
    public abstract class BooleanValue : Value
    {
        public static readonly ConstantBooleanValue True = new ConstantBooleanValue(true);
        public static readonly ConstantBooleanValue False = new ConstantBooleanValue(false);

        protected BooleanValue() : base(BooleanType.Type)
        { }

        public abstract void BuildJump(Function function, Anchor? trueAnchor, Anchor? falseAnchor);
        public override void BuildInstructions(Function function,
            AssignableOperand destinationOperand)
        {
            Anchor trueAnchor = function.CreateAnchor();
            Anchor falseAnchor = function.CreateAnchor();
            Anchor endAnchor = function.CreateAnchor();
            BuildJump(function, trueAnchor, falseAnchor);

            trueAnchor.Address = function.NextAddress;
            function.Instructions.Add(Compiler.CreateLoadInstruction(function, destinationOperand, new BooleanOperand(1)));
            function.Instructions.Add(Compiler.CreateJumpInstruction(function, endAnchor));

            falseAnchor.Address = function.NextAddress;
            function.Instructions.Add(Compiler.CreateLoadInstruction(function, destinationOperand, new BooleanOperand(0)));

            endAnchor.Address = function.NextAddress;
        }

        public override void BuildInstructions(Function function)
        {
            var compiler = Compiler.Instance;
            Anchor trueAnchor = function.CreateAnchor();
            Anchor falseAnchor = function.CreateAnchor();
            Anchor endAnchor = function.CreateAnchor();
            BuildJump(function, trueAnchor, falseAnchor);

            trueAnchor.Address = function.NextAddress;
            function.Instructions.Add(compiler.CreateJumpInstruction(function, endAnchor));
            falseAnchor.Address = function.NextAddress;
            endAnchor.Address = function.NextAddress;
        }

        public override BooleanValue? ToBooleanValue()
        {
            return this;
        }
    }

    public class ConstantBooleanValue : BooleanValue
    {
        public readonly bool Value;

        public ConstantBooleanValue(bool booleanValue)
        {
            Value = booleanValue;
        }

        public override void BuildJump(Function function, Anchor? trueAnchor, Anchor? falseAnchor)
        {
            if (Value && trueAnchor != null) {
                var instruction = Compiler.Instance.CreateJumpInstruction(function, trueAnchor);
                function.Instructions.Add(instruction);
            }
            else if (!Value && falseAnchor != null) {
                var instruction = Compiler.Instance.CreateJumpInstruction(function, falseAnchor);
                function.Instructions.Add(instruction);
            }
        }

        public int ToInteger() => Value ? 1 : 0;
        public override Operand ToOperand(Function function)
        {
            return ToOperand();
        }

        public Operand ToOperand()
        {
            return new BooleanOperand(ToInteger());
        }
    }

    class LogicalNot : BooleanValue
    {
        public readonly BooleanValue SourceValue;

        public LogicalNot(BooleanValue sourceValue)
        {
            SourceValue = sourceValue;
        }

        public override void BuildJump(Function function, Anchor? trueAnchor, Anchor? falseAnchor)
        {
            SourceValue.BuildJump(function, falseAnchor, trueAnchor);
        }
    }

}
