namespace Inu.Cate.Sc62015
{
    internal class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand, List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands) { }

        protected override void Call()
        {
            WriteLine("\tcall " + TargetFunction.Label);
        }

        protected override void StoreParameters()
        {
            StoreParametersDirect();
        }
    }
}
