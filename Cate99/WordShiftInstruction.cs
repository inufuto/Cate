using System;

namespace Inu.Cate.Tms99
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected override void ShiftConstant(int count)
        {
            count &= 15;
            string operation = OperatorId switch
            {
                Keyword.ShiftLeft => "sla",
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => "sra",
                Keyword.ShiftRight => "srl",
                _ => throw new NotImplementedException()
            };
            WordOperation.UsingAnyRegister(this, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                WriteLine("\t" + operation + "\t" + temporaryRegister.Name + "," + count);
            });
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
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
                    RemoveVariableRegister(r0);
                    ChangedRegisters.Add(r0);
                    r0.Store(this, DestinationOperand);
                }

                if (RightOperand.Type.ByteCount == 1) {
                    r1.ByteRegister.Load(this, RightOperand);
                }
                else {
                    r1.Load(this, RightOperand);
                }

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
                WordOperation.UsingRegister(this, r0, Operate);
                return;
            }
            WordOperation.UsingRegister(this, r1, () =>
            {
                WordOperation.UsingRegister(this, r0, Operate);
            });
        }
    }
}
