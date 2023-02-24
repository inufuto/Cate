using System;
using System.Linq;

namespace Inu.Cate.I8086
{
    internal class AddOrSubtractInstruction : Cate.AddOrSubtractInstruction
    {
        public AddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        public override void BuildAssembly()
        {
            if (IsOperatorExchangeable()) {
                if (LeftOperand is ConstantOperand || DestinationOperand.SameStorage(RightOperand)) {
                    ExchangeOperands();
                }
            }

            if (IncrementOrDecrement()) return;

            var operation = OperatorId switch
            {
                '+' => "add ",
                '-' => "sub ",
                _ => throw new NotImplementedException()
            };
            ResultFlags |= Flag.Z;

            if (DestinationOperand.SameStorage(LeftOperand) &&
                DestinationOperand is VariableOperand { Register: null } destinationVariableOperand) {
                var size = DestinationOperand.Type.ByteCount == 1 ? "byte" : "word";
                var destinationAddress = destinationVariableOperand.MemoryAddress();
                var value = RightOperand switch
                {
                    ConstantOperand constantOperand => constantOperand.MemoryAddress(),
                    VariableOperand { Register: { }, Offset: 0 } variableOperand => variableOperand.Register.Name,
                    _ => null
                };
                if (value != null) {
                    WriteLine("\t" + operation + " " + size + " ptr [" + destinationAddress + "]," + value);
                    return;
                }
            }
            if (DestinationOperand.Type.ByteCount == 1) {
                var candidates = ByteRegister.Registers.Where(r => !r.Conflicts(RightOperand)).ToList();
                ByteOperation.UsingAnyRegister(this, candidates, DestinationOperand, LeftOperand, temporaryRegister =>
                {
                    temporaryRegister.Load(this, LeftOperand);
                    temporaryRegister.Operate(this, operation, true, RightOperand);
                    temporaryRegister.Store(this, DestinationOperand);
                    ChangedRegisters.Add(temporaryRegister);
                });
                return;
            }
            WordOperation.UsingAnyRegister(this, DestinationOperand, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                temporaryRegister.Operate(this, operation, true, RightOperand);
                temporaryRegister.Store(this, DestinationOperand);
                ChangedRegisters.Add(temporaryRegister);
            });
        }

        protected override int Threshold() => DestinationOperand.Type.ByteCount == 1 ? 2 : 4;

        protected override void Increment(int count)
        {
            if (LeftOperand.Type.ByteCount == 1) {
                OperateByte("inc ", count);
            }
            else {
                OperateWord("inc ", count);
            }
            ResultFlags |= Flag.Z;
        }

        protected override void Decrement(int count)
        {
            if (LeftOperand.Type.ByteCount == 1) {
                OperateByte("dec ", count);
            }
            else {
                OperateWord("dec ", count);
            }
            ResultFlags |= Flag.Z;
        }

        private void OperateWord(string operation, int count)
        {
            if (DestinationOperand.Equals(LeftOperand)) {
                if (count == 0) return;
                string? operand = null;
                switch (DestinationOperand) {
                    case VariableOperand { Register: { } } variableOperand:
                        operand = variableOperand.Register.ToString();
                        break;
                    case VariableOperand variableOperand:
                        operand = "word ptr [" + variableOperand.MemoryAddress() + "]";
                        break;
                    case IndirectOperand { Register: { } } indirectOperand: {
                            var offset = indirectOperand.Offset;
                            var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
                            operand = "word ptr [" + indirectOperand.Variable.Label + "]";
                            break;
                        }
                }
                if (operand != null) {
                    for (var i = 0; i < count; ++i) {
                        WriteLine("\t" + operation + operand);
                    }
                    return;
                }
            }
            WordOperation.UsingAnyRegister(this, DestinationOperand, LeftOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, LeftOperand);
                for (var i = 0; i < count; ++i) {
                    WriteLine("\t" + operation + temporaryRegister);
                }
                RemoveRegisterAssignment(temporaryRegister);
                temporaryRegister.Store(this, DestinationOperand);
            });
        }
    }
}
