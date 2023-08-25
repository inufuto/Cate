﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate.I8080
{
    internal class WordAddOrSubtractInstruction : AddOrSubtractInstruction
    {
        public WordAddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        public override void BuildAssembly()
        {
            if (LeftOperand is ConstantOperand && !(RightOperand is ConstantOperand) && IsOperatorExchangeable()) {
                ExchangeOperands();
            }
            else {
                if (RightOperand.Register is WordRegister rightRegister) {
                    if (rightRegister.Addable && IsOperatorExchangeable()) {
                        ExchangeOperands();
                    }
                }
            }

            if (IncrementOrDecrement())
                return;

            if (OperatorId == '+' && !Equals(RightOperand.Register, WordRegister.Hl)) {
                void OperateHl(Cate.WordRegister rightRegister)
                {
                    WordRegister.Hl.Load(this, LeftOperand);
                    WriteLine("\tdad\t" + rightRegister);
                    AddChanged(WordRegister.Hl);
                    RemoveRegisterAssignment(WordRegister.Hl);
                    WordRegister.Hl.Store(this, DestinationOperand);
                }

                if (RightOperand.Register is WordRegister rightWordRegister && (Equals(rightWordRegister, WordRegister.De) || Equals(rightWordRegister, WordRegister.Bc))) {
                    OperateHl(rightWordRegister);
                    return;
                }

                var candidates = new List<Cate.WordRegister>() { WordRegister.Bc, WordRegister.De };
                using var reservation = WordOperation.ReserveAnyRegister(this, candidates);
                {
                    var rightRegister = reservation.WordRegister;
                    rightRegister.Load(this, RightOperand);
                    if (Equals(DestinationOperand.Register, WordRegister.Hl)) {
                        OperateHl(rightRegister);
                    }
                    else {
                        using (WordOperation.ReserveRegister(this, WordRegister.Hl)) {
                            OperateHl(rightRegister);
                        }
                    }
                }
                return;
            }

            string lowOperation, highOperation;
            switch (OperatorId) {
                case '+':
                    lowOperation = "add|adi";
                    highOperation = "adc|aci";
                    break;
                case '-':
                    lowOperation = "sub|sui";
                    highOperation = "sbb|sbi";
                    break;
                default:
                    throw new NotImplementedException();
            }
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, Compiler.LowByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, lowOperation, true, Compiler.LowByteOperand(RightOperand));
                ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                ByteRegister.A.Load(this, Compiler.HighByteOperand(LeftOperand));
                ByteRegister.A.Operate(this, highOperation, true, Compiler.HighByteOperand(RightOperand));
                ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
            }
        }

        protected override int Threshold() => 8;

        protected override void Increment(int count)
        {
            IncrementOrDecrement("inx", count);
        }

        protected override void Decrement(int count)
        {
            IncrementOrDecrement("dcx", count);
        }

        private void IncrementOrDecrement(string operation, int count)
        {
            void ViaRegister(Cate.WordRegister r)
            {
                r.Load(this, LeftOperand);
                IncrementOrDecrement(this, operation, r, count);
            }

            if (DestinationOperand.Register is WordRegister destinationRegister) {
                ViaRegister(destinationRegister);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.Registers, LeftOperand);
            ViaRegister(reservation.WordRegister);
            reservation.WordRegister.Store(this, DestinationOperand);
        }

        private static void IncrementOrDecrement(Instruction instruction, string operation, Cate.WordRegister leftRegister, int count)
        {
            Debug.Assert(count >= 0);
            Debug.Assert(leftRegister.High != null);
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + leftRegister.High.Name);
            }
            instruction.RemoveRegisterAssignment(leftRegister);
            instruction.AddChanged(leftRegister);
        }
    }
}
