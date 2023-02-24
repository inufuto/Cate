using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Cate.I8086
{
    internal class CompareInstruction : Cate.CompareInstruction
    {
        public CompareInstruction(Function function, int operatorId, Operand leftOperand, Operand rightOperand,
            Anchor anchor) : base(function, operatorId, leftOperand, rightOperand, anchor)
        {
        }

        protected override void CompareByte()
        {
            if (LeftOperand.Register != null && RightOperand is IntegerOperand { IntegerValue: 0 })
            {
                WriteLine("\tor " + LeftOperand.Register + "," + LeftOperand.Register);
                goto jump;
            }

            if (LeftOperand is VariableOperand { Register: null } leftVariableOperand)
            {
                if (RightOperand is ConstantOperand constantOperand)
                {
                    WriteLine("\tcmp byte ptr [" + leftVariableOperand.MemoryAddress() + "]," +
                              constantOperand.MemoryAddress());
                    goto jump;
                }

                if (RightOperand is VariableOperand { Register: { } } rightVariableOperand)
                {
                    WriteLine("\tcmp [" + leftVariableOperand.MemoryAddress() + "]," + rightVariableOperand.Register);
                    goto jump;
                }
            }

            ByteOperation.UsingAnyRegister(this, ByteRegister.Registers, null, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, "cmp ", false, RightOperand);
            });

            jump:
            Jump();
        }

        protected override void CompareWord()
        {
            if (LeftOperand.Register != null && RightOperand is IntegerOperand { IntegerValue: 0 })
            {
                WriteLine("\tor " + LeftOperand.Register + "," + LeftOperand.Register);
                goto jump;
            }

            if (LeftOperand is VariableOperand { Register: null } leftVariableOperand)
            {
                if (RightOperand is ConstantOperand constantOperand)
                {
                    WriteLine("\tcmp word ptr [" + leftVariableOperand.MemoryAddress() + "]," +
                              constantOperand.MemoryAddress());
                    goto jump;
                }

                if (RightOperand is VariableOperand { Register: { } } rightVariableOperand)
                {
                    WriteLine("\tcmp [" + leftVariableOperand.MemoryAddress() + "]," + rightVariableOperand.Register);
                    goto jump;
                }
            }

            WordOperation.UsingAnyRegister(this, WordOperation.Registers, null, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, "cmp ", false, RightOperand);
            });

            jump:
            Jump();
        }

        private void Jump()
        {
            switch (OperatorId)
            {
                case Keyword.Equal:
                    WriteJumpLine("\tje " + Anchor);
                    break;
                case Keyword.NotEqual:
                    WriteJumpLine("\tjne " + Anchor);
                    break;
                case '<':
                    if (Signed)
                    {
                        WriteJumpLine("\tjl " + Anchor);
                    }
                    else
                    {
                        WriteJumpLine("\tjb " + Anchor);
                    }

                    break;
                case '>':
                    if (Signed)
                    {
                        WriteJumpLine("\tjg " + Anchor);
                    }
                    else
                    {
                        WriteJumpLine("\tja " + Anchor);
                    }

                    break;
                case Keyword.LessEqual:
                    if (Signed)
                    {
                        WriteJumpLine("\tjle " + Anchor);
                    }
                    else
                    {
                        WriteJumpLine("\tjbe " + Anchor);
                    }

                    break;
                case Keyword.GreaterEqual:
                    if (Signed)
                    {
                        WriteJumpLine("\tjge " + Anchor);
                    }
                    else
                    {
                        WriteJumpLine("\tjae " + Anchor);
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
