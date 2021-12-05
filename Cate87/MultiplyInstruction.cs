using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Inu.Cate.MuCom87
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        {
        }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.LoadConstant(this, 0);
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                });
                return;
            }
            if (BitCount == 1) {
                if (!DestinationOperand.SameStorage(LeftOperand)) {
                    ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                    {
                        ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                        ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                        ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                        ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    });
                }

                ByteOperation.UsingRegister(this, ByteRegister.C, () =>
                {
                    ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                    {
                        ByteRegister.A.Load(this, Compiler.HighByteOperand(DestinationOperand));
                        ByteRegister.C.CopyFrom(this, ByteRegister.A);
                        ByteRegister.A.Load(this, Compiler.LowByteOperand(DestinationOperand));
                        Shift(() =>
                        {
                            WriteLine("\tshal");
                            WriteLine("\trcl");
                        });
                        ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                        ByteRegister.A.CopyFrom(this, ByteRegister.C);
                        ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    });
                });
                return;
            }
            //WordOperation.UsingRegister(this, WordRegister.Hl, () =>
            //{
            //    WordRegister.Hl.Load(this, LeftOperand);
            //    ByteOperation.UsingRegister(this, ByteRegister.B, () =>
            //    {
            //        ByteRegister.B.LoadConstant(this, RightValue);
            //        Compiler.CallExternal(this, "cate.Multiply");
            //    });
            //});
            WordOperation.UsingRegister(this, WordRegister.De, () =>
            {
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                    ByteRegister.E.CopyFrom(this, ByteRegister.A);
                    ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                    ByteRegister.D.CopyFrom(this, ByteRegister.A);
                    ChangedRegisters.Add(WordRegister.De);
                    RemoveVariableRegister(WordRegister.De);

                    ByteRegister.A.LoadConstant(this, 0);
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    Operate(() =>
                    {
                        ByteRegister.A.Load(this, Compiler.LowByteOperand(DestinationOperand));
                        WriteLine("\tadd\ta,e");
                        ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                        ByteRegister.A.Load(this, Compiler.HighByteOperand(DestinationOperand));
                        WriteLine("\tadc\ta,d");
                        ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                    }, () =>
                    {
                        Compiler.CallExternal(this, "cate.ShiftLeftDe1");
                    });
                });
            });
        }
    }
}

