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
        public override void AddSourceRegisters()
        {
            AddSourceRegister(SourceOperand);
            if (DestinationOperand is IndirectOperand indirectOperand) {
                AddSourceRegister(indirectOperand);
            }
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
                WordOperation.UsingAnyRegister(this, WordOperation.PairRegisters, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(this, sourceRegister);
                    Debug.Assert(temporaryRegister.Low != null);
                    temporaryRegister.Low.Store(this, DestinationOperand);
                });
                return;
            }
            if (DestinationOperand.Register is ByteRegister destinationRegister) {
                var pairRegister = destinationRegister.PairRegister;
                if (pairRegister != null && Equals(pairRegister.Low, destinationRegister)) {
                    Debug.Assert(pairRegister.High != null);
                    if (!IsRegisterInUse(pairRegister.High)) {
                        pairRegister.Load(this, SourceOperand);
                        return;
                    }
                }
                destinationRegister.Load(this, Compiler.Instance.LowByteOperand(SourceOperand));
                return;
            }
            ByteOperation.UsingAnyRegister(this, DestinationOperand, null, register =>
            {
                var lowByteOperand = Compiler.Instance.LowByteOperand(SourceOperand);
                register.Load(this, lowByteOperand);
                register.Store(this, DestinationOperand);
            });
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
                    Debug.Assert(destinationRegister.Low != null && destinationRegister.High != null);
                    destinationRegister.Low.Load(this, SourceOperand);
                    destinationRegister.High.LoadConstant(this, 0);
                    return;
                }
                //if (SourceOperand.Register is ByteRegister sourceRegister) {
                //    ExpandRegister(destinationRegister, sourceRegister);
                //}
            }
            var pairRegisters = WordOperation.PairRegisters;
            if (DestinationOperand.Register == null) {
                pairRegisters = pairRegisters.Where(r => !IsRegisterInUse(r)).ToList();
            }
            if (pairRegisters.Any()) {
                WordOperation.UsingAnyRegister(this, pairRegisters, DestinationOperand, null, wordRegister =>
                {
                    Debug.Assert(wordRegister.Low != null && wordRegister.High != null);
                    wordRegister.Low.Load(this, SourceOperand);
                    wordRegister.High.LoadConstant(this, 0);
                    wordRegister.Store(this, DestinationOperand);
                });
                return;
            }

            ByteOperation.UsingAnyRegister(this, null, SourceOperand, register =>
            {
                register.Load(this, SourceOperand);
                register.Store(this, Compiler.LowByteOperand(DestinationOperand));
                var highByteOperand = Compiler.HighByteOperand(DestinationOperand);
                ClearHighByte(register, highByteOperand);
            });
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