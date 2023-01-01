using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.MuCom87
{
    internal class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public const string TemporaryByte = MuCom87.Compiler.TemporaryByte;   //"@TempParam";
        private bool accumulatorSaved;

        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand,
            List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands)
        {
            ParameterAssignments.Reverse();
        }

        //protected override void CopyByte(Instruction instruction, Cate.ByteRegister destination, Cate.ByteRegister source)
        //{
        //    if (ParameterAssignments.Any(a => Equals(a.Register, ByteRegister.A))) {
        //        instruction.WriteLine("\tstaw\t" + ByteWorkingRegister.TemporaryByte);
        //        instruction.WriteLine("\tmov\ta," + source.Name);
        //        instruction.WriteLine("\tmov\t" + destination.Name + ",a");
        //        instruction.WriteLine("\tldaw\t" + ByteWorkingRegister.TemporaryByte);
        //    }
        //    else {
        //        base.CopyByte(instruction, destination, source);
        //    }
        //}

        protected override void Call()
        {
            if (accumulatorSaved) {
                WriteLine("\tldaw\t" + TemporaryByte);
            }
            WriteLine("\tcall\t" + TargetFunction.Label);
        }

        //protected override Register? SaveAccumulator(Register register, ParameterAssignment assignment)
        //{
        //    if (Equals(register, ByteRegister.A) && ParameterAssignments.Any(a => !a.Equals(assignment) && !a.Done)) {
        //        WriteLine("\tstaw\t" + TemporaryByte);
        //        accumulatorSaved = true;
        //        return null;
        //    }
        //    return base.SaveAccumulator(register, assignment);
        //}

        protected override void StoreParameters()
        {
            StoreParametersDirect();
        }

        public static Register? ParameterRegister(int index, ParameterizableType type)
        {
            return index switch
            {
                0 when type.ByteCount == 1 => ByteRegister.A,
                0 => WordRegister.Hl,
                1 when type.ByteCount == 1 => ByteRegister.E,
                1 => WordRegister.De,
                2 when type.ByteCount == 1 => ByteRegister.C,
                2 => WordRegister.Bc,
                _ => null
            };
        }

        public static Register? ReturnRegister(int byteCount)
        {
            return byteCount switch
            {
                1 => ByteRegister.A,
                2 => WordRegister.Hl,
                _ => null
            };
        }
    }
}
