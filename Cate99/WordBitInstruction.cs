using System;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class WordBitInstruction : BinomialInstruction
    {
        public WordBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            switch (OperatorId) {
                case '|' when RightOperand is IntegerOperand integerOperand:
                    Tms99.WordOperation.OperateConstant(this, "ori", DestinationOperand, LeftOperand, integerOperand.IntegerValue);
                    return;
                case '|':
                    Tms99.WordOperation.Operate(this, "soc", DestinationOperand, LeftOperand, RightOperand);
                    return;
                case '&':
                    ByteOperation.UsingAnyRegisterToChange(this, DestinationOperand, LeftOperand, destinationRegister =>
                    {
                        var candidates = ByteOperation.Registers.Where(r => !Equals(r, destinationRegister)).ToList();
                        ByteOperation.UsingAnyRegister(this, candidates, null, RightOperand, sourceRegister =>
                        {
                            if (Equals(LeftOperand.Register, destinationRegister)) {
                                destinationRegister.Load(this, LeftOperand);
                                sourceRegister.Load(this, RightOperand);
                            }
                            else {
                                destinationRegister.Load(this, RightOperand);
                                sourceRegister.Load(this, LeftOperand);
                            }

                            WriteLine("\tinv\t" + sourceRegister.Name);
                            ChangedRegisters.Add(sourceRegister);
                            WriteLine("\tszc\t" + sourceRegister.Name + "," + destinationRegister.Name);
                            destinationRegister.Store(this, DestinationOperand);
                        });
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
