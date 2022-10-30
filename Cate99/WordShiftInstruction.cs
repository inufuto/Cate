using System;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftConstant(int count)
        {
            count &= 15;
            var operation = OperatorId switch
            {
                Keyword.ShiftLeft => "sla",
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => "sra",
                Keyword.ShiftRight => "srl",
                _ => throw new NotImplementedException()
            };
            WordOperation.UsingAnyRegister(this, DestinationOperand, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                if (count > 0) {
                    WriteLine("\t" + operation + "\t" + temporaryRegister.Name + "," + count);
                }
                temporaryRegister.Store(this, DestinationOperand);
            });
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            void FromRight(Cate.WordRegister register)
            {
                if (RightOperand.Type.ByteCount == 1) {
                    var wordRegister = ((WordRegister)register);
                    var byteRegister = wordRegister.ByteRegister;
                    byteRegister.Load(this, RightOperand);
                    byteRegister.Expand(this, ((IntegerType)RightOperand.Type).Signed);
                }
                else {
                    register.Load(this, RightOperand);
                }
            }

            var functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeft",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSigned"
                    : "cate.ShiftRight",
                _ => throw new NotImplementedException()
            };
            var r0 = WordRegister.FromIndex(0);
            var r1 = WordRegister.FromIndex(1);
            void Operate()
            {
                void CallShift()
                {
                    r0.Load(this, LeftOperand);
                    Compiler.CallExternal(this, functionName);
                    RemoveRegisterAssignment(r0);
                    ChangedRegisters.Add(r0);
                    r0.Store(this, DestinationOperand);
                }
                FromRight(r1);

                if (Equals(DestinationOperand.Register, r0)) {
                    CallShift();
                }
                else {
                    WordOperation.UsingRegister(this, r0, CallShift);
                }
            }

            if (Equals(DestinationOperand.Register, r0)) {
                WordOperation.UsingRegister(this, r1, Operate);
                return;
            }
            if (Equals(DestinationOperand.Register, r1)) {
                if (Equals(LeftOperand.Register, r1)) {
                    WordOperation.UsingRegister(this, r0, () =>
                    {
                        var candidates = WordRegister.Registers.Where(r => r != null && !Equals(r, r0) && !Equals(r, r1)).ToList();
                        WordOperation.UsingAnyRegister(this, candidates, temporaryRegister =>
                        {
                            FromRight(temporaryRegister);
                            r0.CopyFrom(this, r1);
                            r1.CopyFrom(this, temporaryRegister);
                            Compiler.CallExternal(this, functionName);
                            r0.Store(this, DestinationOperand);
                        });
                    });
                    return;
                }
                WordOperation.UsingRegister(this, r0, Operate);
                return;
            }
            WordOperation.UsingRegister(this, r1, () =>
            {
                WordOperation.UsingRegister(this, r0, Operate);
            });
        }

        public override bool IsCalling() => true;
    }
}
