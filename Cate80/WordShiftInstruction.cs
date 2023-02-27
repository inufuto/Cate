using System;
using System.Diagnostics;

namespace Inu.Cate.Z80
{
    internal class WordShiftInstruction : Cate.WordShiftInstruction
    {
        public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
            Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
        { }

        protected override int Threshold() => 2;
        public override void BuildAssembly()
        {
            if (LeftOperand.Register != null && RightOperand is IntegerOperand { IntegerValue: 8 }) {
                using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters, DestinationOperand, LeftOperand);
                var pairRegister = reservation.WordRegister;
                pairRegister.Load(this, LeftOperand);
                Debug.Assert(pairRegister.High != null);
                Debug.Assert(pairRegister.Low != null);
                switch (OperatorId) {
                    case Keyword.ShiftLeft:
                        pairRegister.High.CopyFrom(this, pairRegister.Low);
                        pairRegister.Low.LoadConstant(this, 0);
                        break;
                    case Keyword.ShiftRight:
                        pairRegister.Low.CopyFrom(this, pairRegister.High);
                        pairRegister.High.LoadConstant(this, 0);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                return;
            }
            base.BuildAssembly();
        }

        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            if (register is WordRegister wordRegister) {
                if (!wordRegister.IsPair())
                    return false;
            }
            return base.CanAllocateRegister(variable, register);
        }

        protected override void ShiftConstant(int count)
        {
            if (DestinationOperand.Register is WordRegister destinationRegister) {
                if (destinationRegister.IsPair()) {
                    destinationRegister.Load(this, LeftOperand);
                    Shift(destinationRegister, count);
                    return;
                }
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, WordRegister.PairRegisters, null, LeftOperand);
            var temporaryRegister = reservation.WordRegister;
            temporaryRegister.Load(this, LeftOperand);
            Shift(temporaryRegister, count);
            temporaryRegister.Store(this, DestinationOperand);
        }

        private void Shift(Cate.WordRegister register, int count)
        {
            Action action = OperatorId switch
            {
                Keyword.ShiftLeft => () =>
                {
                    WriteLine("\tsla\t" + register.Low);
                    WriteLine("\trl\t" + register.High);
                }
                ,
                Keyword.ShiftRight when ((IntegerType)LeftOperand.Type).Signed => () =>
               {
                   WriteLine("\tsra\t" + register.High);
                   WriteLine("\trr\t" + register.Low);
               }
                ,
                Keyword.ShiftRight => () =>
                {
                    WriteLine("\tsrl\t" + register.High);
                    WriteLine("\trr\t" + register.Low);
                }
                ,
                _ => throw new NotImplementedException()
            };

            for (var i = 0; i < count; ++i) {
                action();
            }
            AddChanged(register);
            RemoveRegisterAssignment(register);
        }

        protected override void ShiftVariable(Operand counterOperand)
        {
            var functionName = OperatorId switch
            {
                Keyword.ShiftLeft => "cate.ShiftLeftHl",
                Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                    ? "cate.ShiftRightSignedHl"
                    : "cate.ShiftRightHl",
                _ => throw new NotImplementedException()
            };
            using (WordOperation.ReserveRegister(this, WordRegister.Hl, LeftOperand)) {
                WordRegister.Hl.Load(this, LeftOperand);
                using (ByteOperation.ReserveRegister(this, ByteRegister.B)) {
                    ByteRegister.B.Load(this, counterOperand);
                    Compiler.CallExternal(this, functionName);
                }
                AddChanged(WordRegister.Hl);
                RemoveRegisterAssignment(WordRegister.Hl);
                WordRegister.Hl.Store(this, DestinationOperand);
            }
        }
    }
}