using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Inu.Cate.Tms99
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue) { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                ClearDestination();
                return;
            }

            void OperateRegister(WordRegister sourceRegister, WordRegister destinationRegister)
            {
                if (destinationRegister.Index == 0) {
                    var candidates = WordOperation.Registers.Where(r => ((WordRegister)r).Index != 0).ToList();
                    WordOperation.UsingAnyRegister(this, candidates, temporaryRegister =>
                    {
                        OperateRegister(sourceRegister, (WordRegister)temporaryRegister);
                    });
                    return;
                }

                var highRegister = WordRegister.FromIndex(destinationRegister.Index - 1);
                WordOperation.UsingRegister(this, highRegister, () =>
                {
                    sourceRegister.Load(this, LeftOperand);
                    highRegister.LoadConstant(this, RightValue);
                    WriteLine("\tmpy\t" + sourceRegister.Name + "," + highRegister);
                    destinationRegister.Store(this, DestinationOperand);
                });
            }

            if (LeftOperand.Register is WordRegister leftRegister) {
                if (DestinationOperand.Register is WordRegister destinationRegister) {
                    OperateRegister(leftRegister, destinationRegister);
                    return;
                }
                WordOperation.UsingAnyRegister(this, temporaryRegister =>
                {
                    OperateRegister(leftRegister, (WordRegister)temporaryRegister);
                });
            }
            else {
                WordOperation.UsingAnyRegister(this, sourceRegister =>
                {
                    if (DestinationOperand.Register is WordRegister destinationRegister) {
                        OperateRegister((WordRegister)sourceRegister, destinationRegister);
                        return;
                    }
                    WordOperation.UsingAnyRegister(this, temporaryRegister =>
                    {
                        OperateRegister((WordRegister)sourceRegister, (WordRegister)temporaryRegister);
                    });
                });
            }
        }

        private void ClearDestination()
        {
            Tms99.WordOperation.Operate(this, "clr", DestinationOperand);
        }
    }
}
