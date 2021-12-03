namespace Inu.Cate
{
    internal abstract class Statement
    {
        public abstract void BuildInstructions(Function function);
    }

    abstract class JumpStatement : Statement
    {
        public abstract Anchor Anchor { get; }

        public virtual bool HasOperand() => false;

        public override void BuildInstructions(Function function)
        {
            var instruction = Compiler.Instance.CreateJumpInstruction(function, Anchor);
            function.Instructions.Add(instruction);
        }
    }
}
