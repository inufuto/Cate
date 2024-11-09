using System.Diagnostics;

namespace Inu.Cate.Sm85;

internal class WordRegister(int address) : AbstractWordRegister(MinId + address, AddressToName(address))
{
    private const int MinId = 200;
    public readonly int Address = address;
    private static List<Cate.WordRegister>? registers;

    public static List<Cate.WordRegister> Registers
    {
        get
        {
            if (registers != null) return registers;
            registers = [];
            for (var address = 0; address < 16; address += 2) {
                registers.Add(new WordRegister(address));
            }
            return registers;
        }
    }

    private static string AddressToName(int address)
    {
        return Prefix + address;
    }

    public static WordRegister FromAddress(int address)
    {
        return new WordRegister(address);
    }


    public override void LoadConstant(Instruction instruction, int value)
    {
        if (value == 0) {
            instruction.WriteLine("\txorw\t" + this + "," + this);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            return;
        }
        base.LoadConstant(instruction, value);
    }

    public override void LoadFromMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmovw\t" + this + ",@" + label);
        instruction.RemoveRegisterAssignment(this);
        instruction.AddChanged(this);
    }

    public override void StoreToMemory(Instruction instruction, string label)
    {
        instruction.WriteLine("\tmovw\t@" + label + "," + this);
    }

    public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmovw\t" + this + ",@" + pointerRegister);
        }
        else {
            Debug.Assert(pointerRegister.WordRegister != null);
            if (((WordRegister)pointerRegister.WordRegister).Address == 0) {
                var candidates = PointerRegister.TemporaryRegisters.Where(r => !Equals(r.WordRegister, this)).ToList();
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
                reservation.PointerRegister.CopyFrom(instruction, pointerRegister);
                LoadIndirect(instruction, reservation.PointerRegister, offset);
                return;
            }
            instruction.WriteLine("\tmovw\t" + this + "," + offset + "(" + pointerRegister + ")");
        }
        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
    {
        if (offset == 0) {
            instruction.WriteLine("\tmovw\t" + "@" + pointerRegister + "," + this);
        }
        else {
            instruction.WriteLine("\tmovw\t" + offset + "(" + pointerRegister + ")," + this);
        }
    }

    public override Cate.ByteRegister? Low => ByteRegister.FromAddress(Address + 1);
    public override Cate.ByteRegister? High => ByteRegister.FromAddress(Address);
    public IEnumerable<Register> ByteRegisters
    {
        get
        {
            Debug.Assert(Low != null);
            Debug.Assert(High != null);
            return [Low, High];
        }
    }
}