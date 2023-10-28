﻿using System.Diagnostics;

namespace Inu.Cate.Mc6800;

internal class MultiplyInstruction : Cate.MultiplyInstruction
{
    public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue)
        : base(function, destinationOperand, leftOperand, rightValue) { }

    public override void BuildAssembly()
    {
        if (RightValue == 0) {
            ByteOperation.Operate(this, "clr", true, Compiler.LowByteOperand(DestinationOperand));
            ByteOperation.Operate(this, "clr", true, Compiler.HighByteOperand(DestinationOperand));
            return;
        }

        if (BitCount == 1) {
            if (LeftOperand.SameStorage(DestinationOperand)) {
                if (DestinationOperand is VariableOperand variableOperand) {
                    var variable = variableOperand.Variable;
                    var offset = variableOperand.Offset;
                    Debug.Assert(variable.Register == null);
                    Shift(() =>
                    {
                        WriteLine("\tasl\t" + variable.MemoryAddress(offset + 1));
                        WriteLine("\trol\t" + variable.MemoryAddress(offset));
                    });
                    RemoveVariableRegister(variableOperand);
                    return;
                }
            }

            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                    ByteRegister.B.Load(this, Compiler.LowByteOperand(LeftOperand));
                    Shift(() =>
                    {
                        WriteLine("\taslb");
                        WriteLine("\trola");
                    });
                    ByteRegister.B.Store(this, Compiler.LowByteOperand(DestinationOperand));
                }
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            }
            return;
        }
        WriteLine("\tclr\t" + ZeroPage.WordLow);
        WriteLine("\tclr\t" + ZeroPage.WordHigh);
        IndexRegister.X.Load(this, LeftOperand);
        WriteLine("\tstx\t" + ZeroPage.Word2);
            
        using var reservation = ByteOperation.ReserveAnyRegister(this);
        var register = reservation.ByteRegister;
        Operate(() =>
        {
            register.LoadFromMemory(this, ZeroPage.Word.Low.Name);
            register.Operate(this, "add", true, ZeroPage.Word2.Low.Name);
            register.StoreToMemory(this, ZeroPage.Word.Low.Name);
            register.LoadFromMemory(this, ZeroPage.Word.High.Name);
            register.Operate(this, "adc", true, ZeroPage.Word2.High.Name);
            register.StoreToMemory(this, ZeroPage.Word.High.Name);
        }, () =>
        {
            WriteLine("\tasl\t" + ZeroPage.Word2Low);
            WriteLine("\trol\t" + ZeroPage.Word2High);
        });
        IndexRegister.X.LoadFromMemory(this, ZeroPage.Word.Name);
        IndexRegister.X.Store(this, DestinationOperand);
    }
}