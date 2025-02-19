﻿using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate;

public abstract class WordOperation : RegisterOperation<WordRegister>
{
    public List<WordRegister> PairRegisters => Registers.Where(r => r.IsPair()).ToList();
    public virtual List<WordRegister> PointerRegisters => Registers;

    public virtual RegisterReservation ReserveRegister(Instruction instruction, WordRegister register)
    {
        return instruction.ReserveRegister(register);
    }

    protected virtual bool CanCopyRegisterToSave(Instruction instruction, WordRegister register)
    {
        return ((instruction.ResultOperand == null || !Equals(register, instruction.ResultOperand.Register)) &&
                !instruction.IsRegisterInVariableRange(register, null));
    }

    public RegisterReservation ReserveRegister(Instruction instruction, WordRegister register, Operand operand)
    {
        instruction.CancelOperandRegister(operand);
        return instruction.ReserveRegister(register, operand);
    }

    public RegisterReservation ReserveAnyRegister(Instruction instruction, List<WordRegister> candidates, Operand sourceOperand)
    {
        if (sourceOperand is not VariableOperand variableOperand) return ReserveAnyRegister(instruction, candidates);
        {
            var register = instruction.GetVariableRegister(variableOperand);
            if (register is not WordRegister wordRegister || !candidates.Contains(register))
                return ReserveAnyRegister(instruction, candidates);
            return ReserveRegister(instruction, wordRegister, sourceOperand);
        }
    }


    public RegisterReservation ReserveAnyRegister(Instruction instruction, Operand sourceOperand)
    {
        return ReserveAnyRegister(instruction, RegistersForType(sourceOperand.Type), sourceOperand);
    }

    public virtual List<WordRegister> RegistersForType(Type type)
    {
        return Registers.Where(r => r.ByteCount == type.ByteCount).ToList();
    }

    public RegisterReservation ReserveAnyRegister(Instruction instruction, List<WordRegister> candidates)
    {
        if (Compiler.Instance.IsAssignedRegisterPrior()) {
            foreach (var register in candidates.Where(r => !instruction.IsRegisterReserved(r) && !instruction.IsRegisterInVariableRange(r, null))) {
                return instruction.ReserveRegister(register);
            }
        }
        foreach (var register in candidates.Where(register => !instruction.IsRegisterReserved(register))) {
            return instruction.ReserveRegister(register);
        }

        var savedRegister = candidates.Last();
        return instruction.ReserveRegister(savedRegister);
    }

    public RegisterReservation ReserveAnyRegister(Instruction instruction)
    {
        return ReserveAnyRegister(instruction, Registers);
    }

    public Operand LowByteOperand(Operand operand) => Compiler.LowByteOperand(operand);

    public virtual void StoreConstantIndirect(Instruction instruction, WordRegister pointerRegister, int offset, int value)
    {
        using var reservation = ReserveAnyRegister(instruction, Registers);
        reservation.WordRegister.LoadConstant(instruction, value);
        reservation.WordRegister.StoreIndirect(instruction, pointerRegister, offset);
    }

    public virtual List<WordRegister> RegistersToOffset(int offset)
    {
        return Registers.Where(r => r.IsOffsetInRange(offset)).ToList(); ;
    }

    public virtual void ClearWord(Instruction instruction, string label)
    {
        using var reservation = ReserveAnyRegister(instruction, Registers);
        reservation.WordRegister.LoadConstant(instruction, 0);
        reservation.WordRegister.StoreToMemory(instruction, label);
    }
}