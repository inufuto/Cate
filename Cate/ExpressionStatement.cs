namespace Inu.Cate
{
    class ExpressionStatement : Statement
    {
        private readonly Value value;

        public ExpressionStatement(Value value)
        {
            this.value = value;
        }


        public override void BuildInstructions(Function function)
        {
            value.BuildInstructions(function);
        }
    }
}