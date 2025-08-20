using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate;

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

    public override bool IsSourceOperand(Variable variable)
    {
        return SourceOperand.IsVariable(variable);
    }

    public override List<Operand> SourceOperands => [SourceOperand];

    public override Operand? ResultOperand => DestinationOperand;

    public override string ToString()
    {
        return DestinationOperand + " = " + SourceOperand;
    }
}

public class ByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    : LoadInstruction(function, destinationOperand, sourceOperand)
{
    public override void BuildAssembly()
    {
        if (
            DestinationOperand.SameStorage(SourceOperand) &&
            DestinationOperand.Type.ByteCount == SourceOperand.Type.ByteCount
        ) {
            return;
        }

        if (SourceOperand is IntegerOperand integerOperand) {
            switch (DestinationOperand) {
                case VariableOperand variableOperand when DestinationOperand.Register == null && integerOperand.IntegerValue == 0:
                    ByteOperation.ClearByte(this, variableOperand);
                    return;
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        var register = GetVariableRegister(pointer, 0);
                        if (register is WordRegister pointerRegister && pointerRegister.IsOffsetInRange(0)) {
                            ByteOperation.StoreConstantIndirect(this, pointerRegister, offset, integerOperand.IntegerValue);
                            return;
                        }
                        var pointerRegisters = WordOperation.RegistersToOffset(offset);
                        if (pointerRegisters.Any()) {
                            using var reservation = WordOperation.ReserveAnyRegister(this, pointerRegisters, SourceOperand);
                            reservation.WordRegister.LoadFromMemory(this, pointer, 0);
                            ByteOperation.StoreConstantIndirect(this, reservation.WordRegister, offset, integerOperand.IntegerValue);
                            return;
                        }
                        break;
                    }
            }
        }

        if (DestinationOperand.Register is ByteRegister byteRegister) {
            byteRegister.Load(this, SourceOperand);
            SetVariableRegister(DestinationOperand, byteRegister);
            return;
        }
        using (var reservation = ByteOperation.ReserveAnyRegister(this, Candidates(), SourceOperand)) {
            reservation.ByteRegister.Load(this, SourceOperand);
            reservation.ByteRegister.Store(this, DestinationOperand);
        }
    }

    protected virtual List<ByteRegister> Candidates() => ByteOperation.Registers.Where(r => !IsRegisterReserved(r, SourceOperand)).ToList();
}

public class WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    : LoadInstruction(function, destinationOperand, sourceOperand)
{
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
                WordOperation.StoreConstantIndirect(this, pointerRegister, offset, integerOperand.IntegerValue);
                return;
            }

            var pointerRegisters = WordOperation.RegistersToOffset(offset);
            if (pointerRegisters.Count > 0) {
                using var reservation = WordOperation.ReserveAnyRegister(this, pointerRegisters, SourceOperand);
                reservation.WordRegister.LoadFromMemory(this, pointer, 0);
                WordOperation.StoreConstantIndirect(this, reservation.WordRegister, offset,
                    integerOperand.IntegerValue);
                return;
            }
        }

        if (DestinationOperand.Register is WordRegister destinationRegister) {
            ViaRegister(destinationRegister);
            return;
        }
        if (SourceOperand.Register is WordRegister leftRegister) {
            ViaRegister(leftRegister);
            return;
        }
        {
            using var reservation = WordOperation.ReserveAnyRegister(this, SourceOperand);
            ViaRegister(reservation.WordRegister);
        }
        return;

        void ViaRegister(WordRegister register)
        {
            register.Load(this, SourceOperand);
            register.Store(this, DestinationOperand);
        }
    }
}
