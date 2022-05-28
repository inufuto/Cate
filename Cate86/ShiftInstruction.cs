using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Cate.I8086
{
    internal class ShiftInstruction : Cate.ShiftInstruction
    {
        public ShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        protected override int Threshold() => 4;

        protected override void ShiftConstant(int count)
        {
            Operate(count);
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            ByteOperation.UsingRegister(this, ByteRegister.Cl, () =>
            {
                ByteRegister.Cl.Load(this, counterOperand);
                Operate(null);
            });
        }

        private void Operate(int? count)
        {
            var operation = OperatorId switch
            {
                Keyword.ShiftLeft => "shl ",
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => "sar ",
                Keyword.ShiftRight => "shr ",
                _ => throw new NotImplementedException()
            };

            if (
                count is 1 &&
                DestinationOperand.SameStorage(LeftOperand) &&
                DestinationOperand is VariableOperand { Register: null } destinationVariableOperand
            ) {
                var size = DestinationOperand.Type.ByteCount == 1 ? "byte" : "word";
                var destinationAddress = destinationVariableOperand.MemoryAddress();
                WriteLine("\t" + operation + size + " ptr [" + destinationAddress + "],1");
                return;
            }
            if (DestinationOperand.Type.ByteCount == 1) {
                ByteOperation.UsingAnyRegister(this, DestinationOperand, LeftOperand, temporaryRegister =>
                  {
                      temporaryRegister.Load(this, LeftOperand);
                      if (count != null) {
                          for (var i = 0; i < count; ++i) {
                              WriteLine("\t" + operation + temporaryRegister + ",1");
                          }
                      }
                      else {
                          WriteLine("\t" + operation + temporaryRegister + ",cl");
                      }
                      temporaryRegister.Store(this, DestinationOperand);
                      ChangedRegisters.Add(temporaryRegister);
                  });
                return;
            }
            WordOperation.UsingAnyRegister(this, DestinationOperand, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                if (count != null) {
                    for (var i = 0; i < count; ++i) {
                        WriteLine("\t" + operation + temporaryRegister + ",1");
                    }
                }
                else {
                    WriteLine("\t" + operation + temporaryRegister + ",cl");
                }
                temporaryRegister.Store(this, DestinationOperand);
                ChangedRegisters.Add(temporaryRegister);
            });
        }
    }
}
