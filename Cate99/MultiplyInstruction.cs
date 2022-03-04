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
                return !IsRegisterInUse(highRegister);
            }).ToList();
            WordOperation.UsingAnyRegister(this, candidates, DestinationOperand, LeftOperand, lowRegister =>
            {
                var highRegister = WordRegister.FromIndex(((WordRegister)lowRegister).Index - 1);
                WordOperation.UsingRegister(this, highRegister, () =>
                {
                    WordOperation.UsingAnyRegister(this, rightRegister =>
                    {
                        highRegister.Load(this, LeftOperand);
                        rightRegister.LoadConstant(this, RightValue);
                        WriteLine("\tmpy\t" + rightRegister.Name + "," + highRegister);
                        lowRegister.Store(this, DestinationOperand);
                        ChangedRegisters.Add(lowRegister);
                    });
                });
            });
        }

        private void ClearDestination()
        {
            Tms99.WordOperation.Operate(this, "clr", DestinationOperand);
        }
    }
}
