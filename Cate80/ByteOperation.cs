using System;
using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.Z80;

internal class ByteOperation : Cate.ByteOperation
{
    public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
    public override List<Cate.ByteRegister> Accumulators => ByteRegister.Accumulators;

    protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable,
        int offset, int count)
    {
        var address = variable.MemoryAddress(offset);

        void OperateA()
        {
            ByteRegister.A.Operate(instruction, operation, change, count);
            ByteRegister.A.StoreToMemory(instruction, variable, offset);
        }

        var register = instruction.GetVariableRegister(variable, offset);
        if (Equals(register, ByteRegister.A)) {
            OperateA();
            goto end;
        }
        if (!instruction.IsRegisterReserved(PointerRegister.Hl)) {
            using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                PointerRegister.Hl.LoadConstant(instruction, address);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "(hl)");
                }
            }
            if (change) {
                instruction.RemoveVariableRegister(variable, offset);
            }
            goto end;
        }

        using (ReserveRegister(instruction, ByteRegister.A)) {
            ByteRegister.A.LoadFromMemory(instruction, address);
            OperateA();
        }
        end:
        ;
    }

    protected override void OperateIndirect(Instruction instruction, string operation, bool change,
        Cate.PointerRegister pointerRegister, int offset, int count)
    {
        if (pointerRegister is IndexRegister && pointerRegister.IsOffsetInRange(offset)) {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "(" + pointerRegister + "+" + offset + ")");
            }
            return;
        }
        if (offset == 0) {
            var operand = "(" + pointerRegister + ")";
            if (Equals(pointerRegister, PointerRegister.Hl)) {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + operand);
                }
                return;
            }
            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadFromMemory(instruction, operand);
                ByteRegister.A.Operate(instruction, operation, change, count);
                ByteRegister.A.StoreToMemory(instruction, operand);
            }
            return;
        }
        pointerRegister.TemporaryOffset(instruction, offset, () =>
        {
            OperateIndirect(instruction, operation, change, pointerRegister, 0, count);
        });
    }


    public override void ClearByte(Instruction instruction, string label)
    {
        instruction.RemoveRegisterAssignment(ByteRegister.A);
        instruction.WriteLine("\txor\ta");
        instruction.WriteLine("\tld\t(" + label + "),a");
        instruction.AddChanged(ByteRegister.A);
    }

    public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister,
        int offset, int value)
    {
        if (pointerRegister is IndexRegister && pointerRegister.IsOffsetInRange(offset)) {
            instruction.WriteLine("\tld\t(" + pointerRegister + "+" + offset + ")," + value);
            return;
        }
        if (offset == 0) {
            if (PointerRegister.IsAddable(pointerRegister)) {
                instruction.WriteLine("\tld\t(" + pointerRegister + ")," + value);
                return;
            }
            using (ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadConstant(instruction, value);
                instruction.WriteLine("\tld\t(" + pointerRegister + "),a");
            }
            return;
        }
        if (PointerRegister.IsAddable(pointerRegister)) {
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                StoreConstantIndirect(instruction, pointerRegister, 0, value);
            });
            return;
        }
        List<Cate.PointerRegister> candidates = new List<Cate.PointerRegister>(PointerRegister.PointerOrder(offset).Where(r => !Equals(r, pointerRegister)).ToList());
        using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
        reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
        StoreConstantIndirect(instruction, reservation.PointerRegister, offset, value);
    }

    public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister rightRegister)
    {
        throw new NotImplementedException();
    }
}