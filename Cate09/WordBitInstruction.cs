using System;
using System.Diagnostics;

namespace Inu.Cate.Mc6809
{
    internal class WordBitInstruction : BinomialInstruction
    {
        public WordBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override void BuildAssembly()
        {
            string operation = OperatorId switch
            {
                '|' => "or",
                '^' => "eor",
                '&' => "and",
                _ => throw new NotImplementedException()
            };

            Operand rightLow, rightHigh;
            if (RightOperand.Register is WordRegister rightRegister) {
                WriteLine("\tst" + rightRegister + "\t" + DirectPage.Word);
                rightLow = new StringOperand(IntegerType.ByteType, DirectPage.Word.Low.ToString());
                rightHigh = new StringOperand(IntegerType.ByteType, DirectPage.Word.High.ToString());
            }
            else {
                rightLow = Compiler.LowByteOperand(RightOperand);
                rightHigh = Compiler.HighByteOperand(RightOperand);
            }

            if (LeftOperand.Register is Cate.WordRegister leftRegister) {
                void OperateLeftRegister(Cate.WordRegister wordRegister)
                {
                    var lowRegister = wordRegister.Low;
                    Debug.Assert(lowRegister != null);
                    lowRegister.Operate(this, operation, true, rightLow);

                    var highRegister = wordRegister.High;
                    Debug.Assert(highRegister != null);
                    highRegister.Operate(this, operation, true, rightHigh);

                    wordRegister.Store(this, DestinationOperand);
                }

                if (leftRegister.IsPair()) {
                    OperateLeftRegister(leftRegister);
                    return;
                }
                WordOperation.UsingAnyRegister(this, WordOperation.PairRegisters, DestinationOperand, LeftOperand, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(this, leftRegister);
                    OperateLeftRegister(temporaryRegister);
                });
                return;
            }

            if (DestinationOperand.Register is Cate.WordRegister destinationRegister) {
                void OperateDestinationRegister(Cate.WordRegister wordRegister)
                {
                    wordRegister.Load(this, LeftOperand);

                    var lowRegister = wordRegister.Low;
                    Debug.Assert(lowRegister != null);
                    lowRegister.Operate(this, operation, true, rightLow);

                    var highRegister = wordRegister.High;
                    Debug.Assert(highRegister != null);
                    highRegister.Operate(this, operation, true, rightHigh);
                }

                if (destinationRegister.IsPair()) {
                    OperateDestinationRegister(destinationRegister);
                    return;
                }
                WordOperation.UsingAnyRegister(this, WordOperation.PairRegisters, DestinationOperand, LeftOperand, temporaryRegister =>
                {
                    OperateDestinationRegister(temporaryRegister);
                    destinationRegister.CopyFrom(this, destinationRegister);
                });
                return;
            }

            ByteOperation.UsingAnyRegister(this, register =>
            {
                register.Load(this, Compiler.LowByteOperand(LeftOperand));
                register.Operate(this, operation, true, rightLow);
                register.Store(this, Compiler.LowByteOperand(DestinationOperand));

                register.Load(this, Compiler.HighByteOperand(LeftOperand));
                register.Operate(this, operation, true, rightHigh);
                register.Store(this, Compiler.HighByteOperand(DestinationOperand));
            });
        }
    }
}