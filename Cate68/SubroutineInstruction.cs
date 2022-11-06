using System.Collections.Generic;

namespace Inu.Cate.Mc6800
{
    class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand, List<Operand> sourceOperands)
            : base(function, targetFunction, destinationOperand, sourceOperands) { }

        protected override void Call()
        {
            if (TargetFunction.Visibility == Visibility.External) {
                WriteLine("\tjsr\t" + TargetFunction.Label);
            }
            else {
                WriteLine("\tbsr\t" + TargetFunction.Label);
            }
            RemoveRegisterAssignment(WordRegister.X);
        }

        protected override void StoreParameters()
        {
            StoreParametersDirect();
        }
    }
}