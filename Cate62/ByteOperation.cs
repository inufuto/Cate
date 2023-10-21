namespace Inu.Cate.Sc62015
{
    internal class ByteOperation : Cate.ByteOperation
    {
        private readonly List<Cate.ByteRegister> accumulators = new();
        private readonly List<Cate.ByteRegister> registers = new();

        public ByteOperation()
        {
            accumulators.Add(ByteRegister.A);
            registers.AddRange(ByteRegister.Registers);
            registers.AddRange(ByteInternalRam.Registers);
        }


        public override List<Cate.ByteRegister> Accumulators => accumulators;
        public override List<Cate.ByteRegister> Registers => registers;

        //protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset, int count)
        //{
        //    if (!change) {
        //        var register = instruction.GetVariableRegister(variable, offset);
        //        if (register is Cate.ByteRegister byteRegister) {
        //            byteRegister.Operate(instruction, operation, change, count);
        //            return;
        //        }
        //    }
        //    else if (variable.Register is ByteRegister byteRegister) {
        //        byteRegister.Operate(instruction, operation, change, count);
        //        return;
        //    }
        //    using var reservation = ReserveAnyRegister(instruction, Registers);
        //    instruction.RemoveVariableRegister(variable, offset);
        //    reservation.ByteRegister.LoadFromMemory(instruction, variable, offset);
        //    reservation.ByteRegister.Operate(instruction, operation, change, count);
        //    reservation.ByteRegister.StoreToMemory(instruction, variable, offset);
        //}

        //protected override void OperateIndirect(Instruction instruction, string operation, bool change,
        //    Cate.PointerRegister pointerRegister, int offset,
        //    int count)
        //{
        //    using var reservation = ReserveAnyRegister(instruction, Registers);
        //    reservation.ByteRegister.LoadIndirect(instruction, pointerRegister, offset);
        //    reservation.ByteRegister.Operate(instruction, operation, change, count);
        //    reservation.ByteRegister.StoreIndirect(instruction, pointerRegister, offset);
        //}

        //public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset, int value)
        //{
        //    using var reservation = ReserveAnyRegister(instruction, Registers);
        //    reservation.ByteRegister.LoadConstant(instruction, value);
        //    reservation.ByteRegister.StoreIndirect(instruction, pointerRegister, offset);
        //}


        //public override void ClearByte(Instruction instruction, string label)
        //{
        //    using var reservation = ReserveAnyRegister(instruction, Registers);
        //    reservation.ByteRegister.LoadConstant(instruction, 0);
        //    reservation.ByteRegister.StoreToMemory(instruction, label);
        //}

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
        {
            instruction.WriteLine("\tmv " + Sc62015.Compiler.TemporaryByte + "," + register.AsmName);
            return Sc62015.Compiler.TemporaryByte;
        }
    }
}
