using System;
using System.Diagnostics;

namespace Inu.Cate.Mos6502
{
    class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }



        //public override void BuildAssembly()
        //{
        //    if (ByteChange()) return;

        //    if (DestinationOperand is IndirectOperand) {
        //        ZeroPage.UsingWord(this, temporary =>
        //        {
        //            Compiler.OperateWord(this, LeftOperand, (leftLowOffset, leftLow, leftHighOffset, leftHigh) =>
        //            {
        //                Compiler.WriteYOffset(this, leftLowOffset);
        //                WriteLine("\tlda\t" + leftLow);
        //                WriteLine("\tsta\t" + temporary);
        //                Compiler.WriteYOffset(this, leftHighOffset);
        //                WriteLine("\tlda\t" + leftHigh);
        //                WriteLine("\tsta\t" + temporary + "+1");
        //            });
        //            BuildAssembly(null, temporary, null, temporary + "+1");
        //            Compiler.OperateWord(this, DestinationOperand, (destinationLowOffset, destinationLow, destinationHighOffset, destinationHigh) =>
        //            {
        //                WriteLine("\tlda\t" + temporary);
        //                Compiler.WriteYOffset(this, destinationLowOffset);
        //                WriteLine("\tsta\t" + destinationLow);
        //                WriteLine("\tlda\t" + temporary + "+1");
        //                Compiler.WriteYOffset(this, destinationHighOffset);
        //                WriteLine("\tsta\t" + destinationLow);
        //            });
        //        });
        //    }

        //    Compiler.OperateWord(this, DestinationOperand,
        //        (destinationLowOffset, destinationLow, destinationHighOffset, destinationHigh) =>
        //        {
        //            if (!LeftOperand.SameStorage(DestinationOperand)) {
        //                Compiler.OperateWord(this, LeftOperand, (leftLowOffset, leftLow, leftHighOffset, leftHigh) =>
        //                {
        //                    Compiler.WriteYOffset(this, leftLowOffset);
        //                    WriteLine("\tlda\t" + leftLow);
        //                    Compiler.WriteYOffset(this, destinationLowOffset);
        //                    WriteLine("\tsta\t" + destinationLow);
        //                    Compiler.WriteYOffset(this, leftHighOffset);
        //                    WriteLine("\tlda\t" + leftHigh);
        //                    Compiler.WriteYOffset(this, destinationHighOffset);
        //                    WriteLine("\tsta\t" + destinationHigh);
        //                });
        //            }
        //            BuildAssembly(destinationLowOffset, destinationLow, destinationHighOffset, destinationHigh);
        //        });
        //}

        protected override void ShiftConstant(int count)
        {
            if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
                CallExternal(() => ByteRegister.Y.LoadConstant(this, count));
                return;
            }

            Action<Action<string>, Action<string>> byteAction = OperatorId switch
            {
                Keyword.ShiftLeft => (low, high) =>
                {
                    low("asl");
                    high("rol");
                }
                ,
                Keyword.ShiftRight => (low, high) =>
                {
                    high("\tlsr");
                    low("\tror");
                }
                ,
                _ => throw new NotImplementedException()
            };
            if (LeftOperand.SameStorage(DestinationOperand)) {
                for (var i = 0; i < count; ++i) {
                    byteAction(operation =>
                    {
                        ByteOperation.Operate(this, operation, true, Compiler.LowByteOperand(DestinationOperand));
                    }, operation =>
                    {
                        ByteOperation.Operate(this, operation, true, Compiler.HighByteOperand(DestinationOperand));
                    });
                }
                return;
            }
            WordOperation.UsingAnyRegister(this, WordZeroPage.Registers, DestinationOperand, LeftOperand, zeroPage =>
            {
                zeroPage.Load(this, LeftOperand);
                for (var i = 0; i < count; ++i) {
                    byteAction(operation =>
                    {
                        Debug.Assert(zeroPage.Low != null);
                        zeroPage.Low.Operate(this, operation, true, 1);
                    }, operation =>
                    {
                        Debug.Assert(zeroPage.High != null);
                        zeroPage.High.Operate(this, operation, true, 1);
                    });
                }
                zeroPage.Store(this, DestinationOperand);
            });
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            CallExternal(() => ByteRegister.Y.Load(this, counterOperand));
        }

        private void CallExternal(Action loadY)
        {
            string functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftWord",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedWord"
                    : "cate.ShiftRightWord",
                _ => throw new NotImplementedException()
            };
            WordOperation.UsingAnyRegister(this, WordZeroPage.Registers, DestinationOperand, LeftOperand, zeroPage =>
            {
                zeroPage.Load(this, LeftOperand);
                ByteOperation.UsingRegister(this, ByteRegister.X, () =>
                {
                    WordZeroPage wordZeroPage = (WordZeroPage) zeroPage;
                    ByteRegister.X.LoadConstant(this, wordZeroPage.Label);
                    ByteOperation.UsingRegister(this, ByteRegister.Y, () =>
                   {
                       loadY();
                       Compiler.CallExternal(this, functionName);
                   });
                });
                zeroPage.Store(this, DestinationOperand);
            });
        }

        //private void BuildAssembly(int? destinationLowOffset, string destinationLow, int? destinationHighOffset,
        //    string destinationHigh)
        //{
        //    switch (OperatorId) {
        //        case Keyword.ShiftLeft:
        //            Loop(() =>
        //            {
        //                Compiler.WriteYOffset(this, destinationLowOffset);
        //                WriteLine("\tasl\t" + destinationLow);
        //                Compiler.WriteYOffset(this, destinationHighOffset);
        //                WriteLine("\trol\t" + destinationHigh);
        //            });
        //            break;
        //        case Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed:
        //            ZeroPage.UsingByte(this, sign =>
        //            {
        //                Compiler.WriteYOffset(this, destinationHighOffset);
        //                WriteLine("\tlda\t" + destinationHigh);
        //                WriteLine("\tand\t#$80" + sign);
        //                WriteLine("\tsta\t" + sign);
        //                Loop(() =>
        //                {
        //                    Compiler.WriteYOffset(this, destinationHighOffset);
        //                    WriteLine("\tlda\t" + destinationHigh);
        //                    WriteLine("\tlsr\ta");
        //                    WriteLine("\tora\t" + sign);
        //                    WriteLine("\tsta\t" + destinationHigh);
        //                    Compiler.WriteYOffset(this, destinationLowOffset);
        //                    WriteLine("\trol\t" + destinationLow);
        //                });
        //            });
        //            break;
        //        case Keyword.ShiftRight:
        //            Loop(() =>
        //            {
        //                Compiler.WriteYOffset(this, destinationHighOffset);
        //                WriteLine("\tlsr\t" + destinationHigh);
        //                Compiler.WriteYOffset(this, destinationLowOffset);
        //                WriteLine("\trol\t" + destinationLow);
        //            });
        //            break;
        //        default:
        //            throw new NotImplementedException();
        //    }
        //}

        //private bool ByteChange()
        //{
        //    if (((IntegerType)LeftOperand.Type).Signed || !(RightOperand is IntegerOperand integerOperand) ||
        //        integerOperand.IntegerValue != 8) return false;
        //    Compiler.OperateWord(this, DestinationOperand,
        //        (destinationLowOffset, destinationLow, destinationHighOffset, destinationHigh) =>
        //        {
        //            Compiler.OperateWord(this, LeftOperand, (leftLowOffset, leftLow, leftHighOffset, leftHigh) =>
        //            {
        //                switch (OperatorId) {
        //                    case Keyword.ShiftLeft:
        //                        Compiler.WriteYOffset(this, leftLowOffset);
        //                        WriteLine("\tlda\t" + leftLow);
        //                        Compiler.WriteYOffset(this, destinationHighOffset);
        //                        WriteLine("\tsta\t" + destinationHigh);
        //                        Compiler.WriteYOffset(this, destinationLowOffset);
        //                        WriteLine("\tlda\t#0");
        //                        WriteLine("sta" + destinationLow);
        //                        break;
        //                    case Keyword.ShiftRight:
        //                        Compiler.WriteYOffset(this, leftHighOffset);
        //                        WriteLine("\tlda\t" + leftHigh);
        //                        Compiler.WriteYOffset(this, destinationLowOffset);
        //                        WriteLine("\tsta\t" + destinationLow);
        //                        Compiler.WriteYOffset(this, destinationHighOffset);
        //                        WriteLine("\tlda\t#0");
        //                        WriteLine("\tsta\t" + destinationHigh);
        //                        break;
        //                    default:
        //                        throw new NotImplementedException();
        //                }
        //            });
        //        });
        //    return true;
        //}
    }
}