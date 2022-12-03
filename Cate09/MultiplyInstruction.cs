using System;

namespace Inu.Cate.Mc6809
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                ClearDestination();
                goto end;
            }
            if (BitCount == 1) {
                Shift();
                goto end;
            }

            var lowValue = RightValue & 0xff;
            var highValue = (RightValue >> 8) & 0xff;
            if (LeftOperand.Register is WordRegister leftRegister) {
                WriteLine("\tst" + leftRegister + "\t" + DirectPage.Word);
            }
            else {
                WordOperation.UsingAnyRegister(this, WordRegister.Registers, register =>
                {
                    register.Load(this, LeftOperand);
                    WriteLine("\tst" + register.Name + "\t" + DirectPage.Word);
                });
            }

            void ActToRegister()
            {
                if (lowValue != 0) {
                    WriteLine("\tlda\t" + DirectPage.Word + "+1");
                    WriteLine("\tldb\t#" + lowValue);
                    WriteLine("\tmul");
                    if (highValue != 0) {
                        WriteLine("\tstd" + DirectPage.Word2);
                        WriteLine("\tlda\t" + DirectPage.Word);
                        WriteLine("\tldb\t#" + highValue);
                        WriteLine("\tmul");
                        WriteLine("\taddb\t" + DirectPage.Word2);
                        WriteLine("\tstb\t" + DirectPage.Word2);
                        WriteLine("\tldd\t" + DirectPage.Word2);
                    }
                }
                else {
                    WriteLine("\tlda\t" + DirectPage.Word);
                    WriteLine("\tldb\t#" + highValue);
                    WriteLine("\tmul");
                    WriteLine("\ttfr\tb,a");
                    WriteLine("\tclrb");
                }

                if (DestinationOperand.Register is WordRegister destinationRegister) {
                    if (!Equals(destinationRegister, WordRegister.D)) {
                        WriteLine("\ttfr\td," + destinationRegister.Name);
                    }
                    return;
                }
                WordRegister.D.Store(this, DestinationOperand);
            }

            if (DestinationOperand.Register is WordRegister destinationRegister) {
                if (Equals(destinationRegister, WordRegister.D)) {
                    ActToRegister();
                    goto end;
                }
                WordOperation.UsingRegister(this, WordRegister.D, () =>
                {
                    ActToRegister();
                    WriteLine("\ttfr\td, " + destinationRegister.Name);
                });
                goto end;
            }
            WordOperation.UsingRegister(this, WordRegister.D, ActToRegister);

            end:
            RemoveRegisterAssignment(WordRegister.D);
            ChangedRegisters.Add(WordRegister.D);
        }

        private void ClearDestination()
        {
            if (DestinationOperand.Register is WordRegister destinationRegister) {
                WriteLine("\tld" + destinationRegister.Name + ",#0");
                return;
            }

            WordOperation.UsingAnyRegister(this, WordRegister.PointerOrder, register =>
            {
                register.LoadConstant(this, 0);
                register.Store(this, DestinationOperand);
            });
        }

        private void Shift()
        {
            void Loop(Action action)
            {
                var mask = RightValue;
                while ((mask >>= 1) != 0) {
                    action();
                }
            }

            void ActToRegister()
            {
                Loop(() =>
                {
                    WriteLine("\taslb");
                    WriteLine("\trola");
                });
                if (DestinationOperand.Register is WordRegister destinationRegister) {
                    if (destinationRegister != WordRegister.D) {
                        WriteLine("\ttfr\td," + destinationRegister.Name);
                    }
                }
                else {
                    WordRegister.D.Store(this, DestinationOperand);
                }
            }

            if (LeftOperand.SameStorage(DestinationOperand)) {
                if (DestinationOperand.Register is WordRegister register) {
                    if (Equals(register, WordRegister.D)) {
                        ActToRegister();
                        return;
                    }

                    WordOperation.UsingRegister(this, WordRegister.D, () =>
                    {
                        WriteLine("\ttfr\t" + register.Name + ",d");
                        ActToRegister();
                    });
                    return;
                }

                var lowByteOperand = Compiler.LowByteOperand(DestinationOperand);
                var highByteOperand = Compiler.HighByteOperand(DestinationOperand);
                Loop(() =>
                {
                    ByteOperation.Operate(this, "asl", true, lowByteOperand);
                    ByteOperation.Operate(this, "rol", true, highByteOperand);
                });
                return;
            }
            if (LeftOperand.Register is WordRegister leftRegister) {
                if (!Equals(leftRegister, WordRegister.D)) {
                    WordOperation.UsingRegister(this, WordRegister.D, () =>
                    {
                        WriteLine("\ttfr\t" + leftRegister.Name + ",d");
                        ActToRegister();
                    });
                    return;
                }

                ActToRegister();
                return;
            }
            WordRegister.D.Load(this, LeftOperand);
            ActToRegister();
        }
    }
}