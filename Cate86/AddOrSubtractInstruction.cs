using System;
using System.Diagnostics;
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
                void ViaRegister(Cate.ByteRegister r)
                {
                    r.Load(this, LeftOperand);
                    r.Operate(this, operation, true, RightOperand);
                    AddChanged(r);
                }

                if (DestinationOperand.Register is ByteRegister byteRegister && !Equals(RightOperand.Register, byteRegister)) {
                    ViaRegister(byteRegister);
                    return;
                }
                var candidates = ByteRegister.Registers.Where(r => !r.Conflicts(RightOperand)).ToList();
                using var reservation = ByteOperation.ReserveAnyRegister(this, candidates, LeftOperand);
                ViaRegister(reservation.ByteRegister);
                reservation.ByteRegister.Store(this, DestinationOperand);
                return;
            }

            if (DestinationOperand.Type is PointerType) {
                void ViaRegister(Cate.PointerRegister r)
                {
                    if (LeftOperand.Type is PointerType) {
                        r.Load(this, LeftOperand);
                        r.Operate(this, operation, true, RightOperand);
                    }
                    else 
                    {
                        Debug.Assert(r.WordRegister != null);
                        r.WordRegister.Load(this, LeftOperand);
                        r.WordRegister.Operate(this, operation, true, RightOperand);
                    }
                    AddChanged(r);
                }

                if (DestinationOperand.Register is PointerRegister pointerRegister && !Equals(RightOperand.Register, pointerRegister)) {
                    ViaRegister(pointerRegister);
                    return;
                }

                if (LeftOperand.Type is not PointerType) {
                    using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
                    Debug.Assert(reservation.WordRegister != null);
                    reservation.WordRegister.Load(this, LeftOperand);
                    reservation.WordRegister.Operate(this, operation, true, RightOperand);
                    AddChanged(reservation.WordRegister);
                    reservation.WordRegister.Store(this, DestinationOperand);
                }
                else {
                    using var reservation = PointerOperation.ReserveAnyRegister(this, LeftOperand);
                    ViaRegister(reservation.PointerRegister);
                    reservation.PointerRegister.Store(this, DestinationOperand);
                }
            }
            else {
                void ViaRegister(Cate.WordRegister r)
                {
                    r.Load(this, LeftOperand);
                    r.Operate(this, operation, true, RightOperand);
                    AddChanged(r);
                }

                if (DestinationOperand.Register is WordRegister wordRegister && !Equals(RightOperand.Register, wordRegister)) {
                    ViaRegister(wordRegister);
                    return;
                }
                using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
                ViaRegister(reservation.WordRegister);
                reservation.WordRegister.Store(this, DestinationOperand);
            }
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
                        RemoveVariableRegister(variableOperand);
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
            {
                void ViaRegister(Cate.WordRegister r)
                {
                    r.Load(this, LeftOperand);
                    for (var i = 0; i < count; ++i) {
                        WriteLine("\t" + operation + r);
                    }

                    AddChanged(r);
                    RemoveRegisterAssignment(r);
                }

                if (DestinationOperand.Register is WordRegister wordRegister && !Equals(RightOperand.Register, wordRegister)) {
                    ViaRegister(wordRegister);
                    return;
                }
                using var reservation = WordOperation.ReserveAnyRegister(this, LeftOperand);
                ViaRegister(reservation.WordRegister);
                reservation.WordRegister.Store(this, DestinationOperand);
            }
        }
    }
}
