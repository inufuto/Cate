using System;
using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.Mos6502;

internal class ByteOperation : Cate.ByteOperation
{
    public override List<Cate.ByteRegister> Accumulators => new() { ByteRegister.A };
    public override List<Cate.ByteRegister> Registers => ByteRegister.Registers.Union(ByteZeroPage.Registers).ToList();

    protected override void OperateConstant(Instruction instruction, string operation, string value, int count)
    {
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + "\t#" + value);
        }
    }

    protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable,
        int offset, int count)
    {
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + "\t" + variable.MemoryAddress(offset));
        }
        instruction.RemoveVariableRegister(variable, offset);
        instruction.ResultFlags |= Instruction.Flag.Z;
    }

    protected override void OperateIndirect(Instruction instruction, string operation, bool change,
        WordRegister pointerRegister, int offset, int count)
    {
        if (pointerRegister is not WordZeroPage zeroPage) {
            throw new NotImplementedException();
        }
        while (true) {
            if (pointerRegister.IsOffsetInRange(offset)) {
                Mos6502.Compiler.Instance.OperateIndirect(instruction, operation, zeroPage, offset, count);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            }
            pointerRegister.Add(instruction, offset);
            offset = 0;
        }
    }

    public override void StoreConstantIndirect(Instruction instruction, WordRegister pointerRegister, int offset,
        int value)
    {
        using var reservation = ReserveAnyRegister(instruction);
        var register = reservation.ByteRegister;
        register.LoadConstant(instruction, value);
        register.StoreIndirect(instruction, pointerRegister, offset);
    }

    public override void ClearByte(Cate.ByteLoadInstruction instruction, VariableOperand variableOperand)
    {
        if (variableOperand.Register == null) {
            ClearByte(instruction, variableOperand.MemoryAddress());
            return;
        }
        base.ClearByte(instruction, variableOperand);
    }

    public override void ClearByte(Instruction instruction, string label)
    {
        Mos6502.Compiler.Instance.ClearByte(instruction, label);
    }

    public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister register)
    {
        var label = ByteZeroPage.TemporaryByte;
        register.StoreToMemory(instruction, label);
        return label;
    }
}