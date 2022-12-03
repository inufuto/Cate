using System.Collections.Generic;

namespace Inu.Cate.Mos6502
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        { }

        public override void BuildAssembly()
        {
            var candidates = new List<Cate.ByteRegister>() { ByteRegister.A, ByteRegister.X };
            if (RightValue == 0) {
                ByteOperation.UsingAnyRegister(this, candidates, register =>
                {
                    register.LoadConstant(this, 0);
                    register.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    register.Store(this, Compiler.HighByteOperand(DestinationOperand));
                });
                return;
            }
            if (BitCount == 1) {
                if (!DestinationOperand.SameStorage(LeftOperand)) {
                    ByteOperation.UsingAnyRegister(this, candidates, register =>
                    {
                        register.Load(this, Compiler.LowByteOperand(LeftOperand));
                        register.Store(this, Compiler.LowByteOperand(DestinationOperand));
                        register.Load(this, Compiler.HighByteOperand(LeftOperand));
                        register.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    });
                }
                Shift(() =>
                {
                    ByteOperation.Operate(this, "asl", true, Compiler.LowByteOperand(DestinationOperand));
                    ByteOperation.Operate(this, "rol", true, Compiler.HighByteOperand(DestinationOperand));
                });
                return;
            }

            WordOperation.UsingAnyRegister(this, word =>
            {
                ByteOperation.UsingAnyRegister(this, candidates, register =>
                {
                    register.Load(this, Compiler.LowByteOperand(LeftOperand));
                    register.StoreToMemory(this, word.Name + "+0");
                    register.Load(this, Compiler.HighByteOperand(LeftOperand));
                    register.StoreToMemory(this, word.Name + "+1");
                    ChangedRegisters.Add(word);
                    RemoveRegisterAssignment(word);

                    register.LoadConstant(this, 0);
                    register.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    register.Store(this, Compiler.HighByteOperand(DestinationOperand));
                });
            });
            WordOperation.UsingAnyRegister(this, word =>
            {
                Operate(() =>
                {
                    ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                    {
                        ByteRegister.A.Load(this, Compiler.LowByteOperand(DestinationOperand));
                        ByteRegister.A.Operate(this, "clc|adc", true, word + "+0");
                        ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                        ByteRegister.A.Load(this, Compiler.HighByteOperand(DestinationOperand));
                        ByteRegister.A.Operate(this, "adc", true, word + "+1");
                        ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    });
                }, () =>
                {
                    WriteLine("\tasl\t" + word + "+0");
                    WriteLine("\trol\t" + word + "+1");
                });
            });
        }
    }
}