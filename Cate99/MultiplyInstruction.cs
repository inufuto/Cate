using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue) { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                ClearDestination();
                return;
            }
            
            var candidates = WordOperation.Registers.Where(lowRegister =>
            {
                if (((WordRegister)lowRegister).Index == 0) return false;
                var highRegister = WordRegister.FromIndex(((WordRegister)lowRegister).Index - 1);
                return !IsRegisterReserved(highRegister);
            }).ToList();
            using var low = WordOperation.ReserveAnyRegister(this, candidates, LeftOperand);
            {
                var lowRegister = low.WordRegister;
                var highRegister = WordRegister.FromIndex(((WordRegister)lowRegister).Index - 1);
                using (WordOperation.ReserveRegister(this, highRegister)) {
                    using (var right = WordOperation.ReserveAnyRegister(this)) {
                        var rightRegister = right.WordRegister;
                        highRegister.Load(this, LeftOperand);
                        rightRegister.LoadConstant(this, RightValue);
                        WriteLine("\tmpy\t" + rightRegister.Name + "," + highRegister);
                        lowRegister.Store(this, DestinationOperand);
                        AddChanged(lowRegister);
                    }
                }
            }
        }

        private void ClearDestination()
        {
            Tms99.WordOperation.Operate(this, "clr", DestinationOperand);
        }
    }
}
