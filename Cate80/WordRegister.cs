using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class WordRegister : Cate.WordRegister
    {
        public static List<Cate.WordRegister> Registers = new();

        public static Cate.WordRegister FromId(int id)
        {
            var register = Registers.Find(r => r.Id == id);
            var fromId = register;
            if (fromId != null) {
                return fromId;
            }
            throw new ArgumentOutOfRangeException();
        }

        public readonly bool Addable;

        protected WordRegister(int id, string name, bool addable) : base(id, name)
        {
            Addable = addable;
            Registers.Add(this);
        }

        public Cate.ByteRegister[] ByteRegisters => ByteRegister.Registers.Where(b => Name.Contains((string)b.Name)).ToArray();

        public static readonly PairRegister Hl = new(11, ByteRegister.H, ByteRegister.L, true);
        public static readonly PairRegister De = new(12, ByteRegister.D, ByteRegister.E, false);
        public static readonly PairRegister Bc = new(13, ByteRegister.B, ByteRegister.C, false);
        public static readonly WordRegister Ix = new WordRegister(21, "ix", true);
        public static readonly WordRegister Iy = new WordRegister(22, "iy", true);

        public override bool IsPair() => Equals(this, Hl) || Equals(this, De) || Equals(this, Bc);
        //public override bool IsAddable() => Equals(this, Hl) || Equals(this, Ix) || Equals(this, Iy);
        //public override bool IsIndex() => Equals(this, Ix) || Equals(this, Iy);
        //public override Cate.ByteRegister? Low => ByteRegister.FromName(Name.Substring(1, 1));
        //public override Cate.ByteRegister? High => ByteRegister.FromName(Name.Substring(0, 1));

        public static List<Cate.WordRegister> PairRegisters = Registers.Where(r => r.IsPair()).ToList();
        public static List<Cate.WordRegister> AddableRegisters = new List<Cate.WordRegister>() { Hl, Ix, Iy };
        //public static List<Cate.WordRegister> IndexRegisters = Registers.Where(r => r.IsIndex()).ToList();
        //public static List<Cate.WordRegister> RightOperandOrder = new List<Cate.WordRegister>() { De, Bc, Hl, Ix, Iy };
        //public static List<Cate.WordRegister> OffsetRegisters = new List<Cate.WordRegister>() { De, Bc };


        //public static List<Cate.WordRegister> PointerOrder(int offset)
        //{
        //    if (offset == 0 || offset < -128 || offset > 127)
        //        return new List<Cate.WordRegister>() { Hl, Ix, Iy, De, Bc };
        //    else
        //        return new List<Cate.WordRegister>() { Ix, Iy, Hl, De, Bc };
        //}
        //public static List<Cate.WordRegister> Pointers(int offset)
        //{
        //    return PointerOrder(offset).Where(r => r.IsAddable()).ToList();
        //}

        //public override bool Contains(Cate.ByteRegister byteRegister)
        //{
        //    return Name.Contains(byteRegister.Name);
        //}

        public override bool Conflicts(Register? register)
        {
            if (register is ByteRegister byteRegister && Contains(byteRegister))
                return true;
            return base.Conflicts(register);
        }

        public override bool Matches(Register register)
        {
            return Conflicts(register) || register is ByteRegister byteRegister && Contains(byteRegister);
        }

        //public override bool Contains(Register register)
        //{
        //    return base.Contains(register) || Low!.Equals(register) || High!.Equals(register);
        //}

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpush\t" + Name + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpop\t" + Name + comment);
        }

        public void Add(Instruction instruction, int offset)
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

            if (Addable) {
                void ViaRegister(Cate.WordRegister temporaryRegister)
                {
                    temporaryRegister.LoadConstant(instruction, offset);
                    instruction.WriteLine("\tadd\t" + Name + "," + temporaryRegister);
                }
                var candidates = new List<Cate.WordRegister>() { De, Bc };
                if (candidates.Any(r => !instruction.IsRegisterReserved(r))) {
                    using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
                    ViaRegister(reservation.WordRegister);
                }
                else {
                    instruction.WriteLine("\tpush\t" + De);
                    ViaRegister(De);
                    instruction.WriteLine("\tpop\t" + De);
                }
            }
            else {
                Debug.Assert(IsPair());
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

        //public static void UsingAny(Instruction instruction, List<Cate.WordRegister> candidates,
        //    Action<Cate.WordRegister> action)
        //{
        //    var temporaryRegister = TemporaryRegister(instruction, candidates);
        //    instruction.ReserveRegister(temporaryRegister);
        //    action(temporaryRegister);
        //    instruction.CancelRegister(temporaryRegister);
        //}

        //public static void UsingAny(Instruction instruction, List<Cate.WordRegister> candidates, Operand operand, Action<Cate.WordRegister> action)
        //{
        //    if (operand.Register is WordRegister register) {
        //        if (candidates.Contains(register)) {
        //            action(register);
        //            return;
        //        }
        //    }
        //    UsingAny(instruction, candidates, action);
        //}


        public static Cate.WordRegister TemporaryRegister(Instruction instruction, IEnumerable<Cate.WordRegister> registers)
        {
            var register = registers.First(r => !instruction.IsRegisterReserved(r));
            Debug.Assert(register != null);
            return register;
        }

        //public override bool IsOffsetInRange(int offset)
        //{
        //    if (offset == 0) return true;
        //    if (IsIndex())
        //        return offset >= -128 && offset <= 127;
        //    throw new NotImplementedException();
        //}

        //public override bool IsPointer(int offset)
        //{
        //    if (offset == 0) return true;
        //    if (IsIndex())
        //        return offset >= -128 && offset <= 127;
        //    return false;
        //}

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tld\t" + this + "," + value);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tld\t" + Name + ",(" + label+")");
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            //instruction.RemoveVariableRegisterId(Id);
            instruction.WriteLine("\tld\t(" + label + ")," + Name);
        }


        //public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        //{
        //    instruction.WriteLine("\tld\t" + this + ",(" + variable.MemoryAddress(offset) + ")");
        //    instruction.SetVariableRegister(variable, offset, this);
        //    instruction.AddChanged(this);
        //}

        //public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        //{
        //    var destinationAddress = variable.MemoryAddress(offset);
        //    instruction.WriteLine("\tld\t(" + destinationAddress + ")," + this);
        //    instruction.SetVariableRegister(variable, offset, this);
        //}



        //public override void Store(Instruction instruction, AssignableOperand destinationOperand)
        //{
        //    switch (destinationOperand) {
        //        case VariableOperand destinationVariableOperand: {
        //                var destinationVariable = destinationVariableOperand.Variable;
        //                var destinationOffset = destinationVariableOperand.Offset;
        //                if (destinationVariable.Register is WordRegister destinationRegister) {
        //                    Debug.Assert(destinationOffset == 0);
        //                    if (!Equals(destinationRegister, this)) {
        //                        destinationRegister.CopyFrom(instruction, this);
        //                    }
        //                    instruction.SetVariableRegister(destinationVariable, destinationOffset, destinationRegister);
        //                    return;
        //                }
        //                StoreToMemory(instruction, destinationVariable, destinationOffset);
        //                return;
        //            }
        //        case IndirectOperand destinationIndirectOperand: {
        //                var destinationPointer = destinationIndirectOperand.Variable;
        //                var destinationOffset = destinationIndirectOperand.Offset;
        //                if (destinationPointer.Register is WordRegister destinationPointerRegister) {
        //                    StoreIndirect(instruction,
        //                         destinationPointerRegister, destinationOffset);
        //                    return;
        //                }
        //                using var reservation = WordOperation.ReserveAnyRegister(instruction, Pointers(destinationOffset));
        //                StoreIndirect(instruction, reservation.WordRegister, destinationOffset);
        //                return;
        //            }
        //    }
        //    throw new NotImplementedException();
        //}





        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            if (!IsPair()) {
                var candidates = PairRegisters.Where(r => !r.Conflicts(pointerRegister)).ToList();
                using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
                reservation.WordRegister.LoadIndirect(instruction, pointerRegister, offset);
                CopyFrom(instruction, reservation.WordRegister);
                return;
            }
            Debug.Assert(Low != null && High != null);
            if (pointerRegister is IndexRegister indexRegister && indexRegister.IsOffsetInRange(offset + 1)) {
                Low.LoadIndirect(instruction, indexRegister, offset);
                High.LoadIndirect(instruction, indexRegister, offset + 1);
                return;
            }
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    if (Equals(this, Hl)) {
                        var candidates = ByteRegister.Registers.Where(r => !this.Contains(r)).ToList();
                        using var reservation = ByteOperation.ReserveAnyRegister(instruction, candidates);
                        var byteRegister = reservation.ByteRegister;
                        byteRegister.LoadIndirect(instruction, (PointerRegister)pointerRegister, 0);
                        instruction.WriteLine("\tinc\t" + pointerRegister);
                        High.LoadIndirect(instruction, (PointerRegister)pointerRegister, 0);
                        if (!Conflicts(pointerRegister)) {
                            instruction.WriteLine("\tdec\t" + pointerRegister);
                        }
                        Low.CopyFrom(instruction, byteRegister);
                        return;
                    }
                    Low.LoadIndirect(instruction, (PointerRegister)pointerRegister, 0);
                    instruction.WriteLine("\tinc\t" + pointerRegister);
                    High.LoadIndirect(instruction, (PointerRegister)pointerRegister, 0);
                    instruction.WriteLine("\tdec\t" + pointerRegister);
                    return;
                }
                using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                    ByteRegister.A.LoadIndirect(instruction, (PointerRegister)pointerRegister, 0);
                    Low.CopyFrom(instruction, ByteRegister.A);
                    instruction.WriteLine("\tinc\t" + pointerRegister);
                    ByteRegister.A.LoadIndirect(instruction, (PointerRegister)pointerRegister, 0);
                    High.CopyFrom(instruction, ByteRegister.A);
                    instruction.WriteLine("\tdec\t" + pointerRegister);
                }
                return;
            }
            using (var reservation = PointerOperation.ReserveRegister(instruction, pointerRegister)) {
                pointerRegister.Add(instruction, offset);
                LoadIndirect(instruction, pointerRegister, 0);
            }
        }

        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            if (!IsPair()) {
                var candidates = PairRegisters.Where(r => !r.Conflicts(pointerRegister)).ToList();
                using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
                reservation.WordRegister.CopyFrom(instruction, this);
                reservation.WordRegister.StoreIndirect(instruction, pointerRegister, offset);
                return;
            }
            Debug.Assert(Low != null && High != null);
            if (pointerRegister is IndexRegister indexRegister && indexRegister.IsOffsetInRange(offset + 1)) {
                Low.StoreIndirect(instruction, pointerRegister, offset);
                High.StoreIndirect(instruction, pointerRegister, offset + 1);
                return;
            }
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    Low.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tinc\t" + pointerRegister);
                    High.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tdec\t" + pointerRegister);
                    return;
                }
                using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                    ByteRegister.A.CopyFrom(instruction, Low);
                    ByteRegister.A.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tinc\t" + pointerRegister);
                    ByteRegister.A.CopyFrom(instruction, High);
                    ByteRegister.A.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tdec\t" + pointerRegister);
                }
                return;
            }
            using (PointerOperation.ReserveRegister(instruction, pointerRegister)) {
                pointerRegister.Add(instruction, offset);
                StoreIndirect(instruction, pointerRegister, 0);
            }
        }



        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            if (Equals(this, sourceRegister))
                return;

            if (IsPair() && sourceRegister.IsPair()) {
                Debug.Assert(Low != null && High != null);
                Debug.Assert(sourceRegister.Low != null && sourceRegister.High != null);
                Low.CopyFrom(instruction, sourceRegister.Low);
                High.CopyFrom(instruction, sourceRegister.High);
            }
            else {
                instruction.WriteLine("\tpush\t" + sourceRegister.Name);
                instruction.WriteLine("\tpop\t" + Name);
                instruction.AddChanged(this);
            }
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpush\t" + Name);
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpop\t" + Name);
        }
    }

    internal class PairRegister : WordRegister
    {
        public override Cate.ByteRegister? Low { get; }
        public override Cate.ByteRegister? High { get; }

        protected internal PairRegister(int id, ByteRegister highRegister, ByteRegister lowRegister, bool addable) : base(id, highRegister.Name + lowRegister.Name, addable)
        {
            Low = lowRegister;
            High = highRegister;
        }
    }
}