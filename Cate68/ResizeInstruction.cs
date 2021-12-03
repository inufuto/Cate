namespace Inu.Cate.Mc6800
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
            : base(function, destinationOperand, destinationType, sourceOperand, sourceType) { }


        //protected override void Reduce()
        //{
        //    if (DestinationOperand.Register is ByteRegister destinationRegister) {
        //        destinationRegister.Load(this, Compiler.LowByteOperand(SourceOperand));
        //        return;
        //    }
        //    ByteRegister.UsingAny(this, DestinationOperand, register =>
        //    {
        //        var lowByteOperand = Compiler.LowByteOperand(SourceOperand);
        //        register.Load(this, lowByteOperand);
        //        register.Store(this,  DestinationOperand);
        //    });
        //}

        protected override void ExpandSigned()
        {
            void ViaRegister(Cate.ByteRegister register)
            {
                var lowByteOperand = Compiler.LowByteOperand(DestinationOperand);
                var highByteOperand = Compiler.HighByteOperand(DestinationOperand);
                register.Load(this, SourceOperand);
                register.Store(this, lowByteOperand);
                WriteLine("\tasl" + register);
                WriteLine("\tsta" + register + "\t" + ZeroPage.Byte);
                WriteLine("\tsbc" + register + "\t" + ZeroPage.Byte);
                register.Store(this, highByteOperand);
                ResultFlags = 0;
            }

            if (SourceOperand is VariableOperand sourceVariableOperand) {
                var variable = sourceVariableOperand.Variable;
                var offset = sourceVariableOperand.Offset;
                var register = GetVariableRegister(variable, offset);
                if (register is ByteRegister byteRegister) {
                    ViaRegister(byteRegister);
                    return;
                }
            }
            ByteOperation.UsingAnyRegister(this, ViaRegister);
        }

        //protected override void Expand()
        //{
        //    void ViaRegister(Cate.ByteRegister register)
        //    {
        //        var lowByteOperand = Compiler.LowByteOperand(DestinationOperand);
        //        var highByteOperand = Compiler.HighByteOperand(DestinationOperand);
        //        register.Load(this,  SourceOperand);
        //        register.Store(this,  lowByteOperand);
        //        ByteOperation.Operate(this, "clr", true, highByteOperand);
        //        ResultFlags = 0;
        //    }

        //    if (SourceOperand is VariableOperand sourceVariableOperand) {
        //        var variable = sourceVariableOperand.Variable;
        //        var offset = sourceVariableOperand.Offset;
        //        var register = GetVariableRegister(variable, offset);
        //        if (register is ByteRegister byteRegister) {
        //            ViaRegister(byteRegister);
        //            return;
        //        }
        //    }
        //    ByteOperation.UsingAnyRegister(this, ViaRegister);
        //}

        protected override void ClearHighByte(Cate.ByteRegister register, Operand operand)
        {
            ByteOperation.Operate(this, "clr", true, operand);
        }
    }
}