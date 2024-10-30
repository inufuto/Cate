namespace Inu.Cate.Sm85;

internal class ByteRegister(int address) : AbstractByteRegister(MinId + address, AddressToName(address))
{
    private const int MinId = 100;
    public readonly int Address = address;
    private static List<Cate.ByteRegister>? registers;

    public static List<Cate.ByteRegister> Registers
    {
        get
        {
            if (registers != null) return registers;
            registers = [];
            for (var address = 0; address < 8; address += 2) {
                registers.Add(new ByteRegister(address + 1));
                registers.Add(new ByteRegister(address));
            }
            return registers;
        }
    }

    private static string AddressToName(int address)
    {
        return Prefix + address;
    }
    public static ByteRegister FromAddress(int address)
    {
        return new ByteRegister(address);
    }





    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmov\t" + this + ",@" + label);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmov\t@" + label + "," + this);
    }

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmov\t" + this + ",@" + pointerRegister);
        }
        else {
            if (Equals(pointerRegister, PointerRegister.FromAddress(0))) {
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.TemporaryRegisters);
                reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
                instruction.WriteLine("\tmov\t" + this + "," + offset + "(" + reservation.PointerRegister + ")");
            }
            else {
                instruction.WriteLine("\tmov\t" + this + "," + offset + "(" + pointerRegister + ")");
            }
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmov\t" + "@" + pointerRegister + "," + this);
        }
        else {
            if (Equals(pointerRegister, PointerRegister.FromAddress(0))) {
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.TemporaryRegisters);
                reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
                instruction.WriteLine("\tmov\t" + offset + "(" + reservation.PointerRegister + ")," + this);
                return;
            }
            instruction.WriteLine("\tmov\t" + offset + "(" + pointerRegister + ")," + this);
        }
    }

    public override Cate.WordRegister? PairRegister => WordRegister.FromAddress(Address & 0xfe);

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        switch (operand) {
            case IntegerOperand integerOperand:
                instruction.WriteLine("\t" + operation + "\t" + this + "," + integerOperand.IntegerValue);
                break;
            case VariableOperand variableOperand:
                if (variableOperand.Variable.Register != null) {
                    instruction.WriteLine("\t" + operation + "\t" + this + "," + variableOperand.Variable.Register);
                }
                else {
                    instruction.WriteLine("\t" + operation + "\t" + this + ",@" + variableOperand.MemoryAddress());
                }
                break;
            case IndirectOperand indirectOperand: {
                    var variableRegister = indirectOperand.Variable.Register;
                    if (variableRegister is PointerRegister pointerRegister) {
                        ViaRegister(pointerRegister, indirectOperand.Offset);
                    }
                    else {
                        using var reservation = PointerOperation.ReserveAnyRegister(instruction, operand);
                        reservation.PointerRegister.Load(instruction, operand);
                        ViaRegister(reservation.PointerRegister, indirectOperand.Offset);
                    }
                    break;
                    void ViaRegister(Cate.PointerRegister register, int offset)
                    {
                        if (offset == 0) {
                            instruction.WriteLine("\t" + operation + "\t" + this + ",@" + register);
                        }
                        else {
                            if (Equals(register, PointerRegister.FromAddress(0))) {
                                using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.TemporaryRegisters);
                                reservation.PointerRegister.CopyFrom(instruction, register);
                                instruction.WriteLine("\t" + operation + "\t" + this + "," + offset + "(" + reservation.PointerRegister + ")");
                            }
                            else {
                                instruction.WriteLine("\t" + operation + "\t" + this + "," + offset + "(" + register + ")");
                            }
                        }
                    }
                }
            default:
                throw new NotImplementedException();
        }
        if (change) {
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }
        instruction.ResultFlags |= Instruction.Flag.Z;
    }
}