using System.Collections.Generic;

namespace Inu.Cate.Mos6502;

internal class MultiplyInstruction : Cate.MultiplyInstruction
{
    public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
    { }

    public override void BuildAssembly()
    {
        var candidates = new List<Cate.ByteRegister>() { ByteRegister.A, ByteRegister.X };
        if (RightValue == 0) {
            using var reservation = ByteOperation.ReserveAnyRegister(this, candidates);
            var register = reservation.ByteRegister;
            register.LoadConstant(this, 0);
            register.Store(this, Compiler.LowByteOperand(DestinationOperand));
            register.Store(this, Compiler.HighByteOperand(DestinationOperand));
            return;
        }
        if (BitCount == 1) {
            if (!DestinationOperand.SameStorage(LeftOperand)) {
                using var reservation = ByteOperation.ReserveAnyRegister(this, candidates);
                var register = reservation.ByteRegister;
                register.Load(this, Compiler.LowByteOperand(LeftOperand));
                register.Store(this, Compiler.LowByteOperand(DestinationOperand));
                register.Load(this, Compiler.HighByteOperand(LeftOperand));
                register.Store(this, Compiler.HighByteOperand(DestinationOperand));
            }
            Shift(() =>
            {
                ByteOperation.Operate(this, "asl", true, Compiler.LowByteOperand(DestinationOperand));
                ByteOperation.Operate(this, "rol", true, Compiler.HighByteOperand(DestinationOperand));
            });
            return;
        }
        using (var wordReservation = WordOperation.ReserveAnyRegister(this)) {
            var wordRegister = wordReservation.WordRegister;
            using (var byteReservation = ByteOperation.ReserveAnyRegister(this, candidates, LeftOperand)) {
                var byteRegister = byteReservation.ByteRegister;
                byteRegister.Load(this, Compiler.LowByteOperand(LeftOperand));
                byteRegister.StoreToMemory(this, wordRegister.Name + "+0");
                byteRegister.Load(this, Compiler.HighByteOperand(LeftOperand));
                byteRegister.StoreToMemory(this, wordRegister.Name + "+1");
                AddChanged(wordRegister);
                RemoveRegisterAssignment(wordRegister);

                byteRegister.LoadConstant(this, 0);
                byteRegister.Store(this, Compiler.LowByteOperand(DestinationOperand));
                byteRegister.Store(this, Compiler.HighByteOperand(DestinationOperand));
            }
        }
        using (var reserveAnyRegister = WordOperation.ReserveAnyRegister(this)) {
            var wordRegister = reserveAnyRegister.WordRegister;
            Operate(() =>
            {
                using (ByteOperation.ReserveRegister(this, ByteRegister.A, LeftOperand)) {
                    ByteRegister.A.Load(this, Compiler.LowByteOperand(DestinationOperand));
                    ByteRegister.A.Operate(this, "clc|adc", true, wordRegister + "+0");
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    ByteRegister.A.Load(this, Compiler.HighByteOperand(DestinationOperand));
                    ByteRegister.A.Operate(this, "adc", true, wordRegister + "+1");
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                }
            }, () =>
            {
                WriteLine("\tasl\t" + wordRegister + "+0");
                WriteLine("\trol\t" + wordRegister + "+1");
            });
        }
    }
}