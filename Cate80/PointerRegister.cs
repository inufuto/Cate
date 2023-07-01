using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class PointerRegister : WordPointerRegister
    {
        public static readonly List<Cate.PointerRegister> Registers = new();

        public static readonly PointerRegister Hl = new(Z80.WordRegister.Hl, true);
        public static readonly PointerRegister De = new(Z80.WordRegister.De, false);
        public static readonly PointerRegister Bc = new(Z80.WordRegister.Bc, false);
        public static readonly IndexRegister Ix = new(Z80.WordRegister.Ix);
        public static readonly IndexRegister Iy = new(Z80.WordRegister.Iy);

        public static List<Cate.PointerRegister> PointerOrder(int offset)
        {
            return offset is 0 or < -128 or > 127 ? new List<Cate.PointerRegister> { Hl, Ix, Iy, De, Bc } : new List<Cate.PointerRegister> { Ix, Iy, Hl, De, Bc };
        }

        private readonly bool addable;

        protected PointerRegister(Cate.WordRegister wordRegister, bool addable) : base(wordRegister)
        {
            Registers.Add(this);
            this.addable = addable;
        }

        public override bool IsAddable() => addable;


        public override bool IsOffsetInRange(int offset)
        {
            return offset == 0;
        }


        public override void Add(Instruction instruction, int offset)
        {
            if (offset == 0) { return; }

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

            if (IsAddable()) {
                void ViaRegister(Cate.WordRegister temporaryRegister)
                {
                    temporaryRegister.LoadConstant(instruction, offset);
                    instruction.WriteLine("\tadd\t" + Name + "," + temporaryRegister);
                }
                var candidates = new List<Cate.WordRegister>() { Z80.WordRegister.De, Z80.WordRegister.Bc };
                if (candidates.Any(r => !instruction.IsRegisterReserved(r))) {
                    using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
                    ViaRegister(reservation.WordRegister);
                }
                else {
                    instruction.WriteLine("\tpush\t" + De);
                    ViaRegister(Z80.WordRegister.De);
                    instruction.WriteLine("\tpop\t" + De);
                }
            }
            else {
                Debug.Assert(WordRegister.IsPair());
                using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                    Debug.Assert(WordRegister is { Low: { }, High: { } });
                    ByteRegister.A.CopyFrom(instruction, WordRegister.Low);
                    instruction.WriteLine("\tadd\ta,low " + offset);
                    WordRegister.Low.CopyFrom(instruction, ByteRegister.A);
                    ByteRegister.A.CopyFrom(instruction, WordRegister.High);
                    instruction.WriteLine("\tadc\ta,high " + offset);
                    WordRegister.High.CopyFrom(instruction, ByteRegister.A);
                }
            }
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }
    }

    internal class IndexRegister : PointerRegister
    {
        public IndexRegister(Cate.WordRegister wordRegister) : base(wordRegister, true) { }

        public override bool IsOffsetInRange(int offset)
        {
            return offset is >= -128 and <= 127;
        }
    }

}
