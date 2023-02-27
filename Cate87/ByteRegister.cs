using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.MuCom87
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public static readonly Accumulator A = new Accumulator(1);
        public static readonly ByteRegister D = new ByteRegister(2, "d");
        public static readonly ByteRegister E = new ByteRegister(3, "e");
        public static readonly ByteRegister B = new ByteRegister(4, "b");
        public static readonly ByteRegister C = new ByteRegister(5, "c");
        public static readonly ByteRegister H = new ByteRegister(6, "h");
        public static readonly ByteRegister L = new ByteRegister(7, "l");

        public static List<Cate.ByteRegister> Registers = new List<Cate.ByteRegister>() { A, D, E, B, C, H, L };

        protected ByteRegister(int id, string name) : base(id, name) { }

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

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(Equals(A));
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpush\tv" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(Equals(A));
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpop\tv" + comment);
        }

        public override Cate.WordRegister? PairRegister => WordRegister.Registers.FirstOrDefault(wordRegister => wordRegister.Name.Contains(Name));

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tmvi\t" + Name + "," + value);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            if (value == 0 && Equals(this, A)) {
                instruction.WriteLine("\txra\ta,a");
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            base.LoadConstant(instruction, value);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            var address = variable.MemoryAddress(offset);
            LoadFromMemory(instruction, address);
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov\t" + Name + "," + label);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            var address = variable.MemoryAddress(offset);
            StoreToMemory(instruction, address);
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tmov\t" + label + "," + Name);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            if (pointerRegister.Contains(this)) {
                using var reservation = WordOperation.ReserveAnyRegister(instruction);
                var r = reservation.WordRegister;
                r.CopyFrom(instruction, pointerRegister);
                instruction.AddChanged(r);
                LoadIndirect(instruction, r, offset);
                return;
            }
            switch (pointerRegister) {
                case WordRegister wordRegister when offset == instruction.GetRegisterOffset(wordRegister):
                    LoadIndirect(instruction, wordRegister);
                    return;
                case WordRegister wordRegister:
                    wordRegister.TemporaryOffset(instruction, offset, () =>
                    {
                        LoadIndirect(instruction, wordRegister);
                    });
                    return;
            }
            throw new NotImplementedException();
        }

        public virtual void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister)
        {
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.LoadIndirect(instruction, pointerRegister);
                CopyFrom(instruction, A);
            }
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            switch (pointerRegister) {
                case WordRegister wordRegister when offset == instruction.GetRegisterOffset(wordRegister):
                    StoreIndirect(instruction, wordRegister);
                    return;
                case WordRegister wordRegister:
                    var changed = instruction.IsChanged(pointerRegister);
                    if (Math.Abs(offset) > 2) {
                        pointerRegister.Save(instruction);
                        pointerRegister.Add(instruction, offset);
                        StoreIndirect(instruction, pointerRegister, 0);
                        pointerRegister.Restore(instruction);
                    }
                    else {
                        pointerRegister.Add(instruction, offset);
                        StoreIndirect(instruction, pointerRegister, 0);
                        pointerRegister.Add(instruction, -offset);
                    }
                    if (changed)
                        instruction.AddChanged(pointerRegister);
                    else
                        instruction.RemoveChanged(pointerRegister);
                    //instruction.RemoveRegisterAssignment(pointerRegister);
                    return;
            }
            throw new NotImplementedException();
        }

        public virtual void StoreIndirect(Instruction instruction, Cate.WordRegister wordRegister)
        {
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, this);
                A.StoreIndirect(instruction, wordRegister);
            }
        }


        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            if (Equals(sourceRegister, A)) {
                instruction.WriteLine("\tmov\t" + Name + ",a");
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, sourceRegister);
                instruction.WriteLine("\tmov\t" + Name + ",a");
                instruction.AddChanged(this);
                instruction.RemoveRegisterAssignment(this);
            }
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
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, this);
                A.Operate(instruction, operation, change, operand);
                if (change) {
                    CopyFrom(instruction, A);
                }
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, this);
                A.Operate(instruction, operation, change, operand);
                if (change) {
                    CopyFrom(instruction, A);
                }
            }
        }

        public override void Save(Instruction instruction)
        {
            A.Save(instruction);
            A.CopyFrom(instruction, this);
        }

        public override void Restore(Instruction instruction)
        {
            CopyFrom(instruction, A);
            A.Restore(instruction);
        }

        public static List<Cate.ByteRegister> RegistersOtherThan(ByteRegister register)
        {
            return Registers.FindAll(r => !Equals(r, register));
        }
    }
}
