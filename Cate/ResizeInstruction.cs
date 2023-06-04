using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class ResizeInstruction : Instruction
    {
        public readonly AssignableOperand DestinationOperand;
        public readonly IntegerType DestinationType;
        public readonly Operand SourceOperand;
        public readonly IntegerType SourceType;

        protected ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType,
            Operand sourceOperand, IntegerType sourceType) : base(function)
        {
            DestinationOperand = destinationOperand;
            DestinationType = destinationType;
            SourceOperand = sourceOperand;
            SourceType = sourceType;

            DestinationOperand.AddUsage(function.NextAddress, Variable.Usage.Write);
            SourceOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
        }

        public override string ToString()
        {
            return DestinationOperand + " = (" + DestinationType + ")" + SourceOperand;
        }
        public override void ReserveOperandRegisters()
        {
            ReserveOperandRegister(SourceOperand);
            if (DestinationOperand is IndirectOperand indirectOperand) {
                ReserveOperandRegister(indirectOperand);
            }
        }

        public override bool IsSourceOperand(Variable variable)
        {
            return SourceOperand.IsVariable(variable);
        }

        public override Operand? ResultOperand => DestinationOperand;

        //public override void RemoveDestinationRegister()
        //{
        //    RemoveChangedRegisters(DestinationOperand);
        //}

        public override void BuildAssembly()
        {
            if (SourceType.ByteCount == 1) {
                Debug.Assert(DestinationType.ByteCount != 1);
                if (SourceType.Signed && DestinationType.Signed) {
                    ExpandSigned();
                }
                else {
                    Expand();
                }
            }
            else {
                Debug.Assert(DestinationType.ByteCount == 1);
                Reduce();
            }
        }

        protected virtual void Reduce()
        {
            if (SourceOperand.Register is WordRegister sourceRegister) {
                if (sourceRegister.IsPair()) {
                    Debug.Assert(sourceRegister.Low != null);
                    sourceRegister.Low.Store(this, DestinationOperand);
                    return;
                }

                using var reservation = WordOperation.ReserveAnyRegister(this, WordOperation.PairRegisters);
                reservation.WordRegister.CopyFrom(this, sourceRegister);
                Debug.Assert(reservation.WordRegister.Low != null);
                reservation.WordRegister.Low.Store(this, DestinationOperand);
                return;
            }
            if (DestinationOperand.Register is ByteRegister destinationRegister) {
                var pairRegister = destinationRegister.PairRegister;
                if (pairRegister != null && Equals(pairRegister.Low, destinationRegister)) {
                    Debug.Assert(pairRegister.High != null);
                    if (!IsRegisterReserved(pairRegister.High)) {
                        pairRegister.Load(this, SourceOperand);
                        return;
                    }
                }
                destinationRegister.Load(this, Compiler.Instance.LowByteOperand(SourceOperand));
                return;
            }
            using (var reservation = ByteOperation.ReserveAnyRegister(this)) {
                var register = reservation.ByteRegister;
                var lowByteOperand = Compiler.Instance.LowByteOperand(SourceOperand);
                register.Load(this, lowByteOperand);
                register.Store(this, DestinationOperand);
            }
        }

        protected abstract void ExpandSigned();

        protected virtual void Expand()
        {
            {
                if (SourceOperand.Register is ByteRegister sourceRegister) {
                    var pairRegister = sourceRegister.PairRegister;
                    if (pairRegister != null) {
                        if (Equals(pairRegister.Low, sourceRegister)) {
                            Debug.Assert(pairRegister.High != null);
                            pairRegister.High.LoadConstant(this, 0);
                            pairRegister.Store(this, DestinationOperand);
                            return;
                        }
                    }
                }
            }
            if (DestinationOperand.Register is WordRegister destinationRegister) {
                if (destinationRegister.IsPair()) {
                    Debug.Assert(destinationRegister is { Low: { }, High: { } });
                    //WriteLine("\t; low");
                    destinationRegister.Low.Load(this, SourceOperand);
                    //WriteLine("\t; high");
                    destinationRegister.High.LoadConstant(this, 0);
                    return;
                }
            }
            var pairRegisters = WordOperation.PairRegisters;
            if (DestinationOperand.Register == null) {
                pairRegisters = pairRegisters.Where(r => !IsRegisterReserved(r)).ToList();
            }
            if (pairRegisters.Any()) {
                using var reservation = WordOperation.ReserveAnyRegister(this, pairRegisters, DestinationOperand, null);
                Debug.Assert(reservation.WordRegister.Low != null && reservation.WordRegister.High != null);
                reservation.WordRegister.Low.Load(this, SourceOperand);
                reservation.WordRegister.High.LoadConstant(this, 0);
                reservation.WordRegister.Store(this, DestinationOperand);
                return;
            }
            using (var reservation = ByteOperation.ReserveAnyRegister(this, SourceOperand)) {
                var register = reservation.ByteRegister;
                register.Load(this, SourceOperand);
                register.Store(this, Compiler.LowByteOperand(DestinationOperand));
                var highByteOperand = Compiler.HighByteOperand(DestinationOperand);
                ClearHighByte(register, highByteOperand);
            }
        }

        protected virtual void ClearHighByte(ByteRegister register, Operand operand)
        {
            register.LoadConstant(this, 0);
            register.Store(this, operand);
        }

        public override bool CanAllocateRegister(Variable variable, Register register)
        {
            if (!(DestinationOperand is VariableOperand variableOperand) ||
                !variableOperand.Variable.SameStorage(variable))
                return base.CanAllocateRegister(variable, register);
            if (register is WordRegister wordRegister && !wordRegister.IsPair()) {
                return false;
            }
            return base.CanAllocateRegister(variable, register);
        }
    }
}