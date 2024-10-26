using System.Diagnostics;

namespace Inu.Cate.Sm83;

internal class PointerRegister : WordPointerRegister
{
    public static readonly List<Cate.PointerRegister> Registers = [];
    public static readonly PointerRegister Hl = new(Sm83.WordRegister.Hl, true);
    public static readonly PointerRegister De = new(Sm83.WordRegister.De, false);
    public static readonly PointerRegister Bc = new(Sm83.WordRegister.Bc, false);

    public readonly bool Addable;

    public PointerRegister(Cate.WordRegister wordRegister, bool addable) : base(2, wordRegister)
    {
        Addable = addable;
        Registers.Add(this);
    }
  

    public override bool IsOffsetInRange(int offset)
    {
        return offset == 0;
    }

    public override void Add(Instruction instruction, int offset)
    {
        if (offset == 0) {
            return;
        }

        const int threshold = 4;
        var count = offset & 0xffff;
        if (count <= threshold) {
            Loop("inc");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }

        if (count >= 0x10000 - threshold) {
            count = 0x10000 - count;
            Loop("dec");
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            return;
        }

        void Loop(string operation)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + Name);
            }
        }

        if (Equals(this, Hl)) {
            void ViaRegister(Cate.WordRegister temporaryRegister)
            {
                temporaryRegister.LoadConstant(instruction, offset);
                instruction.WriteLine("\tadd\thl," + temporaryRegister);
            }

            var candidates = new List<Cate.WordRegister>() { Sm83.WordRegister.De, Sm83.WordRegister.Bc };
            if (candidates.Any(r => !instruction.IsRegisterReserved(r))) {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
                ViaRegister(reservation.WordRegister);
            }
            else {
                instruction.WriteLine("\tpush\tde");
                ViaRegister(Sm83.WordRegister.De);
                instruction.WriteLine("\tpop\tde");
            }
        }
        else {
            Debug.Assert(WordRegister.IsPair());
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                Debug.Assert(Low != null && High != null);
                ByteRegister.A.CopyFrom(instruction, Low);
                instruction.WriteLine("\tadd\ta,low " + offset);
                Low.CopyFrom(instruction, ByteRegister.A);
                ByteRegister.A.CopyFrom(instruction, High);
                instruction.WriteLine("\tadc\ta,high " + offset);
                High.CopyFrom(instruction, ByteRegister.A);
            }
        }

        instruction.AddChanged(this);
        instruction.RemoveRegisterAssignment(this);
    }

    public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
    {
        throw new NotImplementedException();
    }

    public override void TemporaryOffset(Instruction instruction, int offset, Action action)
    {
        if (Math.Abs(offset) <= 1) {
            base.TemporaryOffset(instruction, offset, action);
            return;
        }
        var changed = instruction.IsChanged(this);
        if (!changed) {
            Save(instruction);
        }
        Add(instruction, offset);
        action();
        if (!changed) {
            Restore(instruction);
            instruction.RemoveChanged(this);
        }
    }
}