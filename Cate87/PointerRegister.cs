using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate.MuCom87
{
    internal class PointerRegister : WordPointerRegister
    {
        public static readonly List<Cate.PointerRegister> Registers = new();
        public static readonly PointerRegister Hl = new(MuCom87.WordRegister.Hl);
        public static readonly PointerRegister De = new(MuCom87.WordRegister.De);
        public static readonly PointerRegister Bc = new(MuCom87.WordRegister.Bc);

        private PointerRegister(Cate.WordRegister wordRegister) : base(wordRegister)
        {
            Registers.Add(this);
        }

        public override bool IsOffsetInRange(int offset)
        {
            return offset == 0;
        }

        public override void Add(Instruction instruction, int offset)
        {
            if (offset > 0) {
                if (offset < 8) {
                    var count = offset;
                    while (count > 0) {
                        instruction.WriteLine("\tinx\t" + AsmName);
                        --count;
                    }
                    instruction.RemoveRegisterAssignment(this);
                    instruction.AddChanged(this);
                    return;
                }
                using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                    Debug.Assert(Low != null);
                    Debug.Assert(High != null);
                    ByteRegister.A.CopyFrom(instruction, Low);
                    instruction.WriteLine("\tadi\ta,low " + offset);
                    Low.CopyFrom(instruction, ByteRegister.A);
                    ByteRegister.A.CopyFrom(instruction, High);
                    instruction.WriteLine("\taci\ta,high " + offset);
                    High.CopyFrom(instruction, ByteRegister.A);
                }
            }
            else if (offset < 0) {
                if (offset > -8) {
                    var count = -offset;
                    while (count > 0) {
                        instruction.WriteLine("\tdcx\t" + AsmName);
                        --count;
                    }
                    return;
                }
                using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                    Debug.Assert(Low != null);
                    Debug.Assert(High != null);
                    ByteRegister.A.CopyFrom(instruction, Low);
                    instruction.WriteLine("\tsui\ta,low " + -offset);
                    Low.CopyFrom(instruction, ByteRegister.A);
                    ByteRegister.A.CopyFrom(instruction, High);
                    instruction.WriteLine("\tsbi\ta,high " + -offset);
                    High.CopyFrom(instruction, ByteRegister.A);
                }
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new System.NotImplementedException();
        }

        public override void TemporaryOffset(Instruction instruction, int offset, Action action)
        {
            var changed = instruction.IsChanged(this);
            if (Math.Abs(offset) > 2) {
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
            else {
                Add(instruction, offset);
                action();
                if (changed)
                    instruction.AddChanged(this);
                else {
                    Add(instruction, -offset);
                    instruction.RemoveChanged(this);
                }
            }
        }
    }
}
