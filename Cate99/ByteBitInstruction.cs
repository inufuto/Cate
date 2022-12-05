using System;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class ByteBitInstruction : BinomialInstruction
    {
        public ByteBitInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            switch (OperatorId) {
                case '|' when RightOperand is IntegerOperand integerOperand:
                    Tms99.ByteOperation.OperateConstant(this, "ori", DestinationOperand, LeftOperand, ByteRegister.ByteConst(integerOperand.IntegerValue));
                    return;
                case '|':
                    Tms99.ByteOperation.Operate(this, "socb", DestinationOperand, LeftOperand, RightOperand);
                    return;
                case '&' when RightOperand is IntegerOperand integerOperand:
                    Tms99.ByteOperation.OperateConstant(this, "andi", DestinationOperand, LeftOperand, ByteRegister.ByteConst(integerOperand.IntegerValue));
                    return;
                case '&':
                    ByteOperation.UsingAnyRegisterToChange(this, DestinationOperand, LeftOperand, destinationRegister =>
                    {
                        var candidates = ByteOperation.Registers.Where(r => !Equals(r, destinationRegister)).ToList();
                        ByteOperation.UsingAnyRegister(this, candidates, null, RightOperand, sourceRegister =>
                        {
                            if (destinationRegister.Conflicts(LeftOperand)) {
                                destinationRegister.Load(this, LeftOperand);
                                sourceRegister.Load(this, RightOperand);
                            }
                            else {
                                destinationRegister.Load(this, RightOperand);
                                sourceRegister.Load(this, LeftOperand);
                            }

                            WriteLine("\tinv\t" + sourceRegister.Name);
                            ChangedRegisters.Add(sourceRegister);
                            WriteLine("\tszcb\t" + sourceRegister.Name + "," + destinationRegister.Name);
                            destinationRegister.Store(this, DestinationOperand);
                        });
                    });
                    break;
                case '^': {
                        void OperateRegister(ByteRegister register)
                        {
                            register.Load(this, LeftOperand);
                            var right = Tms99.Compiler.OperandToString(this, RightOperand);
                            if (right != null) {
                                WriteLine("\txor\t" + right + "," + register.Name);
                            }
                            else {
                                var candidates = ByteOperation.Registers.Where(r => !Equals(r, register)).ToList();
                                ByteOperation.UsingAnyRegister(this, candidates,
                                    rightRegister =>
                                {
                                    rightRegister.Load(this, RightOperand);
                                    WriteLine("\txor\t" + rightRegister.Name + "," + register.Name);
                                });
                            }
                            register.Store(this, DestinationOperand);
                        }
                        if (DestinationOperand.Register is ByteRegister destinationRegister) {
                            OperateRegister(destinationRegister);
                        }
                        else if (LeftOperand.Register is ByteRegister leftRegister) {
                            OperateRegister(leftRegister);
                        }
                        else {
                            ByteOperation.UsingAnyRegister(this, temporaryRegister =>
                            {
                                OperateRegister((ByteRegister)temporaryRegister);
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
