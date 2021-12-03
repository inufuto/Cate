using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate
{
    public abstract class LoadInstruction : Instruction
    {
        public readonly AssignableOperand DestinationOperand;
        public readonly Operand SourceOperand;

        protected LoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function)
        {
            DestinationOperand = destinationOperand;
            SourceOperand = sourceOperand;

            DestinationOperand.AddUsage(function.NextAddress, Variable.Usage.Write);
            SourceOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
        }

        public override void AddSourceRegisters()
        {
            AddSourceRegister(SourceOperand);
            if (DestinationOperand is IndirectOperand indirectOperand) {
                AddSourceRegister(indirectOperand);
            }
        }

        //public override void RemoveDestinationRegister()
        //{
        //    RemoveChangedRegisters(DestinationOperand);
        //}

        public override Operand? ResultOperand => DestinationOperand;

        public override string ToString()
        {
            return DestinationOperand + " = " + SourceOperand;
        }
    }

    public abstract class ByteLoadInstruction : LoadInstruction
    {
        protected ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (
                DestinationOperand.SameStorage(SourceOperand) &&
                DestinationOperand.Type.ByteCount == SourceOperand.Type.ByteCount
            ) {
                return;
            }

            ByteOperation.UsingAnyRegister(this, Candidates(), DestinationOperand, SourceOperand, register =>
            {
                register.Load(this, SourceOperand);
                register.Store(this, DestinationOperand);
            });
        }

        protected virtual List<ByteRegister> Candidates() => ByteOperation.Registers;
    }

    public abstract class WordLoadInstruction : LoadInstruction
    {
        protected WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (
                DestinationOperand.SameStorage(SourceOperand) &&
                DestinationOperand.Type.ByteCount == SourceOperand.Type.ByteCount
            ) {
                return;
            }

            WordOperation.UsingAnyRegister(this, DestinationOperand, SourceOperand, register =>
            {
                register.Load(this, SourceOperand);
                register.Store(this, DestinationOperand);
            });
        }
    }
}