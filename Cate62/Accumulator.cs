namespace Inu.Cate.Sc62015
{
    internal class Accumulator : ByteRegister
    {
        public Accumulator(string name, WordRegister wordRegister) : base(name, wordRegister) { }
        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + Name);
            }
            instruction.AddChanged(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (operand is ConstantOperand constantOperand) {
                Operate(instruction, operation, change, constantOperand.MemoryAddress());
            }
            else {
                using var reservation = ByteOperation.ReserveAnyRegister(instruction,
                    ByteOperation.RegistersOtherThan(ByteRegister.A), operand);
                var operandRegister = reservation.ByteRegister;
                operandRegister.Load(instruction, operand);
                Operate(instruction, operation, change, operandRegister.Name);
            }
        }


        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            instruction.WriteLine("\t" + operation + " " + Name + "," + operand);
            if (change) {
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
        }
    }
}
