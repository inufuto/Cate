using System;

namespace Inu.Cate.Tms99
{
    internal class ByteBitInstruction : BinomialInstruction
    {
        public ByteBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            switch (OperatorId) {
                case '|' when RightOperand is IntegerOperand integerOperand:
                    Tms99.ByteOperation.OperateConstant(this, "ori", DestinationOperand, LeftOperand, integerOperand.IntegerValue);
                    return;
                case '|':
                    Tms99.ByteOperation.Operate(this, "soc", DestinationOperand, LeftOperand, RightOperand);
                    return;
                case '&' when RightOperand is IntegerOperand integerOperand:
                    Tms99.ByteOperation.OperateConstant(this, "andi", DestinationOperand, LeftOperand, integerOperand.IntegerValue);
                    return;
                case '&':
                    ByteOperation.UsingAnyRegister(this, rightRegister =>
                    {
                        rightRegister.Load(this, RightOperand);
                        WriteLine("\tinv\t" + rightRegister.Name);
                        if (DestinationOperand.SameStorage(LeftOperand)) {
                            WriteLine("\tszc\t" + rightRegister.Name + "," + Tms99.Compiler.OperandToString(this, DestinationOperand));
                        }
                        else {
                            ByteOperation.UsingAnyRegisterToChange(this, DestinationOperand, LeftOperand, register =>
                            {
                                register.Load(this, LeftOperand);
                                WriteLine("\tszc\t" + rightRegister.Name + "," + register.Name);
                                register.Store(this, DestinationOperand);
                            });
                        }
                    });
                    break;
                case '^': {
                        void OperateRegister(WordRegister register)
                        {
                            register.Load(this, LeftOperand);
                            WriteLine("\txor\t" + Tms99.Compiler.OperandToString(this, RightOperand) + "," + register.Name);
                            register.Store(this, DestinationOperand);
                        }
                        if (DestinationOperand.Register is WordRegister destinationRegister) {
                            OperateRegister(destinationRegister);
                        }
                        else if (LeftOperand.Register is WordRegister leftRegister) {
                            OperateRegister(leftRegister);
                        }
                        else {
                            WordOperation.UsingAnyRegister(this, temporaryRegister =>
                            {
                                OperateRegister((WordRegister)temporaryRegister);
                            });
                        }

                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
