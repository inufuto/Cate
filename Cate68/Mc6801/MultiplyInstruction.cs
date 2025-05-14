using System;
using System.Diagnostics;

namespace Inu.Cate.Mc6800.Mc6801;

internal class MultiplyInstruction : Cate.MultiplyInstruction
{
    public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue) { }

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
                    RemoveVariableRegister(LeftOperand);
                    return;
                }
            }
            using (WordOperation.ReserveRegister(this, PairRegister.D)) {
                PairRegister.D.Load(this, LeftOperand);
                Shift(() =>
                {
                    WriteLine("\tasld");
                });
                ByteRegister.B.Store(this, Compiler.LowByteOperand(DestinationOperand));
            }
            return;
        }
        using (WordOperation.ReserveRegister(this, PairRegister.D)) {
            ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
            ByteRegister.B.LoadConstant(this, RightValue);
            WriteLine("\tmul");
            PairRegister.D.StoreToMemory(this,  ZeroPage.Word.Name);
            ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
            ByteRegister.B.LoadConstant(this, RightValue);
            WriteLine("\tmul");
            WriteLine("\ttba");
            WriteLine("\tadda\t" + ZeroPage.WordHigh.Name);
            ByteRegister.B.LoadFromMemory(this,  ZeroPage.WordLow.Name);
            PairRegister.D.Store(this, DestinationOperand);
        }
    }
}