using System.Diagnostics;

namespace Inu.Cate.Sm83;

internal class ByteRegister(int id, string name) : Cate.ByteRegister(id, name)
{
    public static readonly ByteRegister A = new(1, "a");
    public static readonly ByteRegister D = new(2, "d");
    public static readonly ByteRegister E = new(3, "e");
    public static readonly ByteRegister B = new(4, "b");
    public static readonly ByteRegister C = new(5, "c");
    public static readonly ByteRegister H = new(6, "h");
    public static readonly ByteRegister L = new(7, "l");

    public static List<Cate.ByteRegister> Registers = [A, D, E, B, C, H, L];
    public static List<Cate.ByteRegister> Accumulators => [A];

    public override Cate.WordRegister? PairRegister => WordRegister.Registers.FirstOrDefault(wordRegister => wordRegister.Contains(this));

    public override bool Conflicts(Register? register)
    {
        switch (register) {
            case WordRegister wordRegister:
                if (wordRegister.Contains(this))
                    return true;
                break;
            case ByteRegister byteRegister:
                if (PairRegister != null && PairRegister.Contains(byteRegister))
                    return true;
                break;
        }
        return base.Conflicts(register);
    }

    public override bool Matches(Register register)
    {
        switch (register) {
            case WordRegister wordRegister:
                if (wordRegister.Contains(this))
                    return true;
                break;
        }
        return base.Matches(register);

    }

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Debug.Assert(Equals(A));
        Instruction.WriteTabs(writer, tabCount);
        if (instruction != null && instruction.IsJump()) {
            writer.WriteLine("\tld\t(" + Compiler.TemporaryByte + "),a" + comment);
        }
        else {
            writer.WriteLine("\tpush\taf" + comment);
        }
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Debug.Assert(Equals(A));
        Instruction.WriteTabs(writer, tabCount);
        if (instruction != null && instruction.IsJump()) {
            writer.WriteLine("\tld\t(" + Compiler.TemporaryByte + "),a" + comment);
        }
        else {
            writer.WriteLine("\tpop\taf" + comment);
        }
    }

    public override void Save(Instruction instruction)
    {
        if (Equals(A)) {
            instruction.WriteLine("\tpush\taf");
            return;
        }
        throw new NotImplementedException();
    }

    public override void Restore(Instruction instruction)
    {
        if (Equals(A)) {
            instruction.WriteLine("\tpop\taf");
            return;
        }
        throw new NotImplementedException();
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\tld\t" + Name + "," + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        if (Equals(A)) {
            instruction.WriteLine("\tld\ta,(" + label + ")");
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            return;
        }
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.LoadFromMemory(instruction, label);
            CopyFrom(instruction, A);
        }
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        if (Equals(A)) {
            instruction.WriteLine("\tld\t(" + label + "),a");
            return;
        }
        using (ByteOperation.ReserveRegister(instruction, A)) {
            A.CopyFrom(instruction, this);
            A.StoreToMemory(instruction, label);
        }
    }

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            if (Equals(pointerRegister, PointerRegister.Hl) || Equals(this, A)) {
                Write(this);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, A)) {
                Write(A);
                CopyFrom(instruction, A);
            }
            return;
        }

        if (pointerRegister.Conflicts(this)) {
            using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.Registers.Where(r => !Equals(r, pointerRegister)).ToList());
            var temporaryRegister = reservation.PointerRegister;
            temporaryRegister.CopyFrom(instruction, pointerRegister);
            temporaryRegister.TemporaryOffset(instruction, offset, () =>
            {
                LoadIndirect(instruction, (Cate.PointerRegister)temporaryRegister, 0);
            });
            return;
        }

        pointerRegister.TemporaryOffset(instruction, offset, () =>
        {
            LoadIndirect(instruction, pointerRegister, 0);
        });
        return;

        void Write(Cate.ByteRegister register)
        {
            instruction.WriteLine("\tld\t" + register + ",(" + pointerRegister + ")");
            instruction.AddChanged(register);
            instruction.RemoveRegisterAssignment(register);
        }
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            if (Equals(pointerRegister, PointerRegister.Hl) || Equals(this, A)) {
                instruction.WriteLine("\tld\t(" + pointerRegister + ")," + this);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, this);
                instruction.WriteLine("\tld\t(" + pointerRegister + ")," + A);
            }
            return;
        }
        pointerRegister.TemporaryOffset(instruction, offset, () =>
        {
            StoreIndirect(instruction, pointerRegister, 0);
        });
        return;
    }

    public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
    {
        if (Equals(sourceRegister, this)) return;

        instruction.WriteLine("\tld\t" + this + "," + sourceRegister);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, int count)
    {
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + Name);
        }
        instruction.AddChanged(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (!Equals(this, A))
            throw new NotImplementedException();
        switch (operand) {
            case IntegerOperand integerOperand:
                instruction.WriteLine("\t" + operation + "\t" + AsmName + "," + integerOperand.IntegerValue);
                instruction.RemoveRegisterAssignment(A);
                return;
            case StringOperand stringOperand:
                instruction.WriteLine("\t" + operation + "\t" + AsmName + "," + stringOperand.StringValue);
                instruction.RemoveRegisterAssignment(A);
                return;
            case ByteRegisterOperand registerOperand:
                instruction.WriteLine("\t" + operation + "\t" + AsmName + "," + registerOperand.Register.AsmName);
                instruction.RemoveRegisterAssignment(A);
                return;
            case VariableOperand variableOperand: {
                    var variable = variableOperand.Variable;
                    var offset = variableOperand.Offset;
                    var register = instruction.GetVariableRegister(variableOperand);
                    if (register is ByteRegister byteRegister) {
                        Debug.Assert(offset == 0);
                        instruction.WriteLine("\t" + operation + "\t" + AsmName + "," + byteRegister);
                        return;
                    }

                    using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
                        WordRegister.Hl.LoadConstant(instruction, variable.MemoryAddress(offset));
                        instruction.WriteLine("\t" + operation + "\t" + AsmName + "," + "(hl)");
                    }
                    return;
                }
            case IndirectOperand indirectOperand: {
                    var pointer = indirectOperand.Variable;
                    var offset = indirectOperand.Offset;
                    {
                        var register = instruction.GetVariableRegister(pointer, 0);
                        if (register is PointerRegister pointerRegister) {
                            OperateAccumulatorIndirect(instruction, operation, pointerRegister, offset);
                            instruction.RemoveRegisterAssignment(this);
                            return;
                        }
                    }
                    using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerOperation.RegistersToOffset(offset));
                    reservation.PointerRegister.LoadFromMemory(instruction, pointer, 0);
                    OperateAccumulatorIndirect(instruction, operation, reservation.PointerRegister, offset);
                    return;
                }
        }
        throw new NotImplementedException();
    }

    public override void Operate(Instruction instruction, string operation, bool change, string operand)
    {
        throw new NotImplementedException();
    }

    private static void OperateAccumulatorIndirect(Instruction instruction, string operation,
        Cate.PointerRegister pointerRegister,
        int offset)
    {
        if (!((PointerRegister)pointerRegister).Addable) {
            using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerOperation.RegistersToOffset(offset));
            reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
            OperateAccumulatorIndirect(instruction, operation, reservation.PointerRegister, offset);
            return;
        }
        if (offset == 0 && Equals(pointerRegister, PointerRegister.Hl)) {
            instruction.WriteLine("\t" + operation + "\ta,(" + pointerRegister + ")");
            return;
        }
        pointerRegister.TemporaryOffset(instruction, offset, () =>
        {
            OperateAccumulatorIndirect(instruction, operation, pointerRegister, 0);
        });
    }
}