using System.Collections.Generic;

namespace Inu.Cate.Mos6502
{
    class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand, List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands)
        { }

        protected override void Call()
        {
            WriteLine("\tjsr\t" + TargetFunction.Label);
            RemoveRegisterAssignment(ByteRegister.X);
            RemoveRegisterAssignment(ByteRegister.Y);
        }

        protected override void StoreParameters()
        {
            StoreParametersDirect();
        }

        protected override void StoreWord(Operand operand, string label)
        {
            ByteOperation.UsingAnyRegister(this, ByteRegister.Registers, register =>
            {
                register.Load(this, Compiler.LowByteOperand(operand));
                register.StoreToMemory(this, label + "+0");
                register.Load(this, Compiler.HighByteOperand(operand));
                register.StoreToMemory(this, label + "+1");
            });
        }

        protected override List<Cate.ByteRegister> Candidates(Operand operand)
        {
            return operand switch
            {
                IndirectOperand _ => new List<Cate.ByteRegister>() { ByteRegister.A },
                _ => base.Candidates(operand)
            };
        }

        public static Register? ParameterRegister(int index, ParameterizableType type)
        {
            //if (index == 0) {
            //    return type.ByteCount == 1 ? ByteZeroPage.First : WordZeroPage.First;
            //}
            //if (index == 0) {
            //    return type.ByteCount == 1 ? (Register)ByteRegister.Y : PairRegister.Xy;
            //}
            return null;
        }


        public static Register ReturnRegister(int byteCount)
        {
            return byteCount == 1 ? (Register)ByteRegister.Y : PairRegister.Xy;
        }
    }
}