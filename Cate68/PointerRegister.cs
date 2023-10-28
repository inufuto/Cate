using System.Collections.Generic;

namespace Inu.Cate.Mc6800
{
    internal class PointerRegister : Cate.WordPointerRegister
    {
        public PointerRegister(Cate.WordRegister wordRegister) : base(2, wordRegister)
        { }

        public static PointerRegister X = new(Mc6800.IndexRegister.X);
        public static List<Cate.PointerRegister> Registers => new() { X };

        public override bool IsOffsetInRange(int offset) => offset is >= 0 and <= 0xff;

        public override void LoadIndirect(Instruction instruction, Variable pointer, int offset)
        {
            WordRegister.LoadIndirect(instruction, pointer, offset);
        }

        public override void StoreIndirect(Instruction instruction, Variable pointer, int offset)
        {
            WordRegister.StoreIndirect(instruction, pointer, offset);
        }

        public override void Add(Instruction instruction, int offset)
        {
            if (offset == 0)
                return;
            if (offset is > 0 and <= 16) {
                while (offset > 0) {
                    instruction.WriteLine("\tinx");
                    --offset;
                }
                instruction.RemoveRegisterAssignment(X);
                return;
            }
            if (offset is < 0 and >= -16) {
                while (offset < 0) {
                    instruction.WriteLine("\tdex");
                    ++offset;
                }
                instruction.RemoveRegisterAssignment(X);
                return;
            }

            if (offset is >= 0 and < 0x100) {
                void AddByte(Cate.ByteRegister byteRegister)
                {
                    byteRegister.LoadConstant(instruction, offset);
                    instruction.Compiler.CallExternal(instruction, "Cate.AddX" + byteRegister.Name.ToUpper());
                    instruction.RemoveRegisterAssignment(X);
                }
                if (!instruction.IsRegisterReserved(ByteRegister.A)) {
                    using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                        AddByte(ByteRegister.A);
                        instruction.RemoveRegisterAssignment(ByteRegister.A);
                    }
                    return;
                }
                if (!instruction.IsRegisterReserved(ByteRegister.B)) {
                    using (ByteOperation.ReserveRegister(instruction, ByteRegister.B)) {
                        AddByte(ByteRegister.B);
                        instruction.RemoveRegisterAssignment(ByteRegister.B);
                    }
                    return;
                }
                using var reservation = ByteOperation.ReserveAnyRegister(instruction);
                AddByte(reservation.ByteRegister);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                ByteRegister.A.LoadConstant(instruction, "high " + offset);
                using (ByteOperation.ReserveRegister(instruction, ByteRegister.B)) {
                    ByteRegister.B.LoadConstant(instruction, "low " + offset);
                    instruction.Compiler.CallExternal(instruction, "Cate.AddXAB");
                    instruction.RemoveRegisterAssignment(X);
                    instruction.RemoveRegisterAssignment(ByteRegister.B);
                }
                instruction.RemoveRegisterAssignment(ByteRegister.A);
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new System.NotImplementedException();
        }
    }
}
