﻿namespace Inu.Cate.Sc62015;

internal class PointerRegister(string name) : Cate.WordRegister(Compiler.NewRegisterId(), 3, name)
{
    public static readonly PointerRegister X = new("x");
    public static readonly PointerRegister Y = new("y");
    public static readonly List<Cate.WordRegister> Registers = [X, Y];

    public virtual string MV => "mv";

    public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpushs " + AsmName + "\t" + comment);
    }

    public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpops " + AsmName + "\t" + comment);
    }

    public override void Save(Instruction instruction)
    {
        instruction.WriteLine("\tpushs " + AsmName + "\t");
    }

    public override void Restore(Instruction instruction)
    {
        instruction.WriteLine("\tpops " + AsmName + "\t");
    }

    public override void LoadConstant(Instruction instruction, string value)
    {
        instruction.WriteLine("\t" + MV + " " + AsmName + "," + value);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\t" + MV + " " + AsmName + ",[" + label + "]");
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\t" + MV + " [" + label + "]," + AsmName);
    }

    public override void LoadIndirect(Instruction instruction, Cate.WordRegister wordRegister, int offset)
    {
        if (wordRegister is PointerRegister pointerRegister) {
            LoadIndirect(instruction, pointerRegister, offset);
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(instruction, Registers);
            reservation.WordRegister.CopyFrom(instruction, wordRegister);
            LoadIndirect(instruction, (PointerRegister)reservation.WordRegister, offset);
        }
    }

    public override void StoreIndirect(Instruction instruction, Cate.WordRegister wordRegister, int offset)
    {
        if (wordRegister is PointerRegister pointerRegister) {
            StoreIndirect(instruction, pointerRegister, offset);
        }
        else {
            using var reservation = WordOperation.ReserveAnyRegister(instruction, Registers);
            reservation.WordRegister.CopyFrom(instruction, wordRegister);
            StoreIndirect(instruction, (PointerRegister)reservation.WordRegister, offset);
        }
    }

    private void LoadIndirect(Instruction instruction, PointerRegister pointerRegister, int offset)
    {
        if (pointerRegister.IsOffsetInRange(offset)) {
            instruction.WriteLine("\t" + MV + " " + AsmName + ",[" + pointerRegister.AsmName + Compiler.OffsetToString(offset) + "]");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }
        else {
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                LoadIndirect(instruction, pointerRegister, 0);
            });
        }
    }

    private void StoreIndirect(Instruction instruction, PointerRegister pointerRegister, int offset)
    {
        if (pointerRegister.IsOffsetInRange(offset)) {
            instruction.WriteLine("\t" + MV + " [" + pointerRegister.AsmName + Compiler.OffsetToString(offset) +
                                  "]," + AsmName);
        }
        else {
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                StoreIndirect(instruction, pointerRegister, 0);
            });
        }
    }

    public override bool IsOffsetInRange(int offset)
    {
        return Math.Abs(offset) < 0x100;
    }

    public override void Add(Instruction instruction, int offset)
    {
        switch (offset) {
            case 0:
                return;
            case > 0 and <= 3: {
                    while (offset > 0) {
                        instruction.WriteLine("\tinc " + AsmName);
                        --offset;
                    }

                    break;
                }
            case < 0 and >= -3: {
                    while (offset < 0) {
                        instruction.WriteLine("\tdec " + AsmName);
                        ++offset;
                    }

                    break;
                }
            default:
                if (this is PointerInternalRam) {
                    using var reservation = WordOperation.ReserveAnyRegister(instruction, Registers);
                    reservation.WordRegister.Add(instruction, offset);
                    CopyFrom(instruction, reservation.WordRegister);
                }
                else {
                    if (Math.Abs(offset) < 0x100) {
                        using var reservation = ByteOperation.ReserveAnyRegister(instruction, ByteRegister.Registers);
                        var byteRegister = reservation.ByteRegister;
                        byteRegister.LoadConstant(instruction, offset);
                        instruction.WriteLine("\tadd " + AsmName + "," + byteRegister.AsmName);
                    }
                    else {
                        using var reservation = WordOperation.ReserveAnyRegister(instruction, Sc62015.WordRegister.Registers);
                        var wordRegister = reservation.WordRegister;
                        wordRegister.LoadConstant(instruction, offset);
                        instruction.WriteLine("\tadd " + AsmName + "," + wordRegister.AsmName);
                    }
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                }
                break;
        }
    }


    public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
    {
        //if (sourceRegister is WordInternalRam wordInternalRam) {
        //    using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers);
        //    reservation.WordRegister.CopyFrom(instruction, wordInternalRam);
        //    CopyFrom(instruction, reservation.WordRegister);
        //    return;
        //}
        if (sourceRegister is WordRegister or WordInternalRam) {
            using var reservation = WordOperation.ReserveAnyRegister(instruction, PointerInternalRam.Registers);
            reservation.WordRegister.CopyFrom(instruction, this);
            reservation.WordRegister.CopyFrom(instruction, sourceRegister);
            CopyFrom(instruction, reservation.WordRegister);
            return;
        }
        var mv = MV;
        if (sourceRegister is not PointerInternalRam) mv = "mv";
        instruction.WriteLine("\t" + mv + " " + AsmName + "," + sourceRegister.AsmName);
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        if (operand is VariableOperand variableOperand) {
            var operandRegister = instruction.GetVariableRegister(variableOperand);
            if (operandRegister != null && (operandRegister is not PointerInternalRam && operandRegister is not WordInternalRam && operandRegister is not ByteInternalRam)) {
                instruction.WriteLine("\t" + operation + " " + AsmName + "," + operandRegister.AsmName);
                goto exit;
            }
        }
        using (var reservation =
               WordOperation.ReserveAnyRegister(instruction, Sc62015.WordRegister.Registers, operand)) {
            reservation.WordRegister.Load(instruction, operand);
            instruction.WriteLine("\t" + operation + " " + AsmName + "," + reservation.WordRegister.AsmName);
        }
    exit:
        if (change) {
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }
    }
}