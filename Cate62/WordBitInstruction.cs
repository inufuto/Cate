namespace Inu.Cate.Sc62015
{
    internal class WordBitInstruction : BinomialInstruction
    {
        public WordBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        {
        }

        public override void BuildAssembly()
        {
            if (LeftOperand is ConstantOperand && RightOperand is not ConstantOperand) {
                ExchangeOperands();
            }

            var operation = OperatorId switch
            {
                '|' => "or\t",
                '^' => "xor\t",
                '&' => "and\t",
                _ => throw new ArgumentException(OperatorId.ToString())
            };

            void ViaInternalRam(WordInternalRam leftRegister)
            {
                if (RightOperand is ConstantOperand constantOperand) {
                    var value = constantOperand.MemoryAddress();
                    WriteLine("\t" + operation + " " + leftRegister.Label + ",low " + value);
                    WriteLine("\t" + operation + " " + leftRegister.Label + "+1,high " + value);
                }
                else {
                    using var reservation = WordOperation.ReserveAnyRegister(this, WordInternalRam.RegistersOtherThan(leftRegister), RightOperand);
                    var rightRegister = (WordInternalRam)reservation.WordRegister;
                    WriteLine("\t" + operation + " " + leftRegister.Label + ",(" + rightRegister.Label + ")");
                    WriteLine("\t" + operation + " " + leftRegister.Label + "+1,(" + rightRegister.Label + "+1)");
                }
            }

            {
                if (DestinationOperand.Register is WordInternalRam leftInternalRam) {
                    ViaInternalRam(leftInternalRam);
                }
                else {
                    using var reservation = WordOperation.ReserveAnyRegister(this, WordInternalRam.Registers, LeftOperand);
                    ViaInternalRam((WordInternalRam)reservation.WordRegister);
                }
            }
        }
    }
}
