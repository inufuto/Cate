using System.Collections.Generic;
using System.Linq;

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

        public override void ReserveOperandRegisters()
        {
            ReserveOperandRegister(SourceOperand);
            if (DestinationOperand is IndirectOperand indirectOperand) {
                ReserveOperandRegister(indirectOperand);
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

            if (SourceOperand is IntegerOperand integerOperand && DestinationOperand is IndirectOperand indirectOperand) {
                var pointer = indirectOperand.Variable;
                var offset = indirectOperand.Offset;
                var register = GetVariableRegister(pointer, 0);
                if (register is WordRegister pointerRegister) {
                    ByteOperation.StoreConstantIndirect(this, pointerRegister, offset, integerOperand.IntegerValue);
                    return;
                }
                var pointerRegisters = WordOperation.PointerRegisters(offset);
                if (pointerRegisters.Any()) {
                    using var reservation = WordOperation.ReserveAnyRegister(this, pointerRegisters, DestinationOperand, SourceOperand);
                    reservation.WordRegister.LoadFromMemory(this, pointer, 0);
                    ByteOperation.StoreConstantIndirect(this, reservation.WordRegister, offset, integerOperand.IntegerValue);
                    return;
                }
            }
            using (var reservation = ByteOperation.ReserveAnyRegister(this, Candidates(), DestinationOperand, SourceOperand)) {
                reservation.ByteRegister.Load(this, SourceOperand);
                reservation.ByteRegister.Store(this, DestinationOperand);
            }
        }

        protected virtual List<ByteRegister> Candidates() => ByteOperation.Registers;
    }

    public class WordLoadInstruction : LoadInstruction
    {
        public WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (
                DestinationOperand.SameStorage(SourceOperand) &&
                DestinationOperand.Type.ByteCount == SourceOperand.Type.ByteCount
            ) {
                return;
            }
            using var reservation= WordOperation.ReserveAnyRegister(this, DestinationOperand, SourceOperand);
            reservation.WordRegister.Load(this, SourceOperand);
            reservation.WordRegister.Store(this, DestinationOperand);
        }
    }
}