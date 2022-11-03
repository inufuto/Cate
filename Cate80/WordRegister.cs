using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class WordRegister : Cate.WordRegister
    {
        public static List<Cate.WordRegister> Registers = new List<Cate.WordRegister>();

        public static Cate.WordRegister FromId(int id)
        {
            var register = Registers.Find(r => r.Id == id);
            var fromId = register;
            if (fromId != null) {
                return fromId;
            }
            throw new ArgumentOutOfRangeException();
        }

        protected WordRegister(int id, string name) : base(id, name)
        {
            Registers.Add(this);
        }

        public Cate.ByteRegister[] ByteRegisters => ByteRegister.Registers.Where(b => Name.Contains((string)b.Name)).ToArray();

        public static readonly WordRegister Hl = new PairRegister(11, ByteRegister.H, ByteRegister.L);
        public static readonly WordRegister De = new PairRegister(12, ByteRegister.D, ByteRegister.E);
        public static readonly WordRegister Bc = new PairRegister(13, ByteRegister.B, ByteRegister.C);
        public static readonly WordRegister Ix = new IndexRegister(21, "ix");
        public static readonly WordRegister Iy = new IndexRegister(22, "iy");

        public override bool IsPair() => Equals(this, Hl) || Equals(this, De) || Equals(this, Bc);
        public override bool IsAddable() => Equals(this, Hl) || Equals(this, Ix) || Equals(this, Iy);
        public override bool IsIndex() => Equals(this, Ix) || Equals(this, Iy);

        //public override Cate.ByteRegister? Low => ByteRegister.FromName(Name.Substring(1, 1));
        //public override Cate.ByteRegister? High => ByteRegister.FromName(Name.Substring(0, 1));

        public static List<Cate.WordRegister> PairRegisters = Registers.Where(r => r.IsPair()).ToList();
        public static List<Cate.WordRegister> AddableRegisters = new List<Cate.WordRegister>() { Hl, Ix, Iy };
        public static List<Cate.WordRegister> IndexRegisters = Registers.Where(r => r.IsIndex()).ToList();
        public static List<Cate.WordRegister> RightOperandOrder = new List<Cate.WordRegister>() { De, Bc, Hl, Ix, Iy };
        public static List<Cate.WordRegister> OffsetRegisters = new List<Cate.WordRegister>() { De, Bc };


        public static List<Cate.WordRegister> PointerOrder(int offset)
        {
            if (offset == 0 || offset < -128 || offset > 127)
                return new List<Cate.WordRegister>() { Hl, Ix, Iy, De, Bc };
            else
                return new List<Cate.WordRegister>() { Ix, Iy, Hl, De, Bc };
        }
        public static List<Cate.WordRegister> Pointers(int offset)
        {
            return PointerOrder(offset).Where(r => r.IsAddable()).ToList();
        }

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

        public override void Add(Instruction instruction, int offset)
        {
            if (offset == 0) { return; }

            const int threshold = 4;
            var count = offset & 0xffff;
            if (count <= threshold) {
                Loop("inc");
                instruction.ChangedRegisters.Add(this);
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            if (count >= 0x10000 - threshold) {
                count = 0x10000 - count;
                Loop("dec");
                instruction.ChangedRegisters.Add(this);
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
                var candidates = new List<Cate.WordRegister>() { De, Bc };
                if (candidates.Any(r => !instruction.IsRegisterInUse(r))) {
                    UsingAny(instruction, candidates, ViaRegister);
                }
                else {
                    instruction.WriteLine("\tpush\t" + De);
                    ViaRegister(De);
                    instruction.WriteLine("\tpop\t" + De);
                }
            }
            else {
                Debug.Assert(IsPair());
                ByteRegister.UsingAccumulator(instruction, () =>
                {
                    Debug.Assert(Low != null && High != null);
                    ByteRegister.A.CopyFrom(instruction, Low);
                    instruction.WriteLine("\tadd\ta,low " + offset);
                    Low.CopyFrom(instruction, ByteRegister.A);
                    ByteRegister.A.CopyFrom(instruction, High);
                    instruction.WriteLine("\tadc\ta,high " + offset);
                    High.CopyFrom(instruction, ByteRegister.A);
                });
            }
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public static void UsingAny(Instruction instruction, List<Cate.WordRegister> candidates,
            Action<Cate.WordRegister> action)
        {
            var temporaryRegister = TemporaryRegister(instruction, candidates);
            instruction.BeginRegister(temporaryRegister);
            action(temporaryRegister);
            instruction.EndRegister(temporaryRegister);
        }

        public static void UsingAny(Instruction instruction, List<Cate.WordRegister> candidates, Operand operand, Action<Cate.WordRegister> action)
        {
            if (operand.Register is WordRegister register) {
                if (candidates.Contains(register)) {
                    action(register);
                    return;
                }
            }
            UsingAny(instruction, candidates, action);
        }


        public static Cate.WordRegister TemporaryRegister(Instruction instruction, IEnumerable<Cate.WordRegister> registers)
        {
            var register = registers.First(r => !instruction.IsRegisterInUse(r));
            Debug.Assert(register != null);
            return register;
        }

        public static void Using(Instruction instruction, WordRegister register, Action action)
        {
            void InvokeAction()
            {
                instruction.BeginRegister(register);
                action();
                instruction.EndRegister(register);
            }
            if (instruction.IsRegisterInUse(register)) {
                var candidates = Registers.Where(r => !Equals(r, register)).ToList();
                UsingAny(instruction, candidates, otherRegister =>
                {
                    otherRegister.CopyFrom(instruction, register);
                    InvokeAction();
                    register.CopyFrom(instruction, otherRegister);
                });
                return;
            }
            InvokeAction();
        }

        public override bool IsOffsetInRange(int offset)
        {
            if (Equals(this, Hl))
                return offset == 0;
            if (IsIndex())
                return offset >= -128 && offset <= 127;
            throw new NotImplementedException();
        }

        public override bool IsPointer(int offset)
        {
            if (Equals(this, Hl))
                return offset == 0;
            if (IsIndex())
                return offset >= -128 && offset <= 127;
            return false;
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tld\t" + this + "," + value);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tld\t" + Name + "," + label);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tld\t" + this + ",(" + variable.MemoryAddress(offset) + ")");
            instruction.SetVariableRegister(variable, offset, this);
            instruction.ChangedRegisters.Add(this);
        }
        private void Store(Instruction instruction, Variable variable, int offset)
        {
            var destinationAddress = variable.MemoryAddress(offset);
            instruction.WriteLine("\tld\t(" + destinationAddress + ")," + this);
            instruction.SetVariableRegister(variable, offset, this);
        }


        public override void Load(Instruction instruction, Operand sourceOperand)
        {
            switch (sourceOperand) {
                case IntegerOperand sourceIntegerOperand:
                    var value = sourceIntegerOperand.IntegerValue;
                    if (instruction.IsConstantAssigned(this, value)) return;
                    LoadConstant(instruction, value.ToString());
                    instruction.SetRegisterConstant(this, value);
                    return;
                case PointerOperand sourcePointerOperand:
                    if (instruction.IsConstantAssigned(this, sourcePointerOperand)) return;
                    instruction.WriteLine("\tld\t" + this + "," + sourcePointerOperand.MemoryAddress());
                    instruction.SetRegisterConstant(this, sourcePointerOperand);
                    instruction.ChangedRegisters.Add(this);
                    return;
                case VariableOperand sourceVariableOperand: {
                        var sourceVariable = sourceVariableOperand.Variable;
                        var sourceOffset = sourceVariableOperand.Offset;
                        if (sourceVariable.Register is WordRegister sourceRegister) {
                            Debug.Assert(sourceOffset == 0);
                            if (!Equals(sourceRegister, this)) {
                                CopyFrom(instruction, sourceRegister);
                            }
                            return;
                        }
                        LoadFromMemory(instruction, sourceVariable, sourceOffset);
                        return;
                    }
                case IndirectOperand sourceIndirectOperand: {
                        var pointer = sourceIndirectOperand.Variable;
                        var offset = sourceIndirectOperand.Offset;
                        if (pointer.Register is WordRegister pointerRegister) {
                            LoadIndirect(instruction, pointerRegister, offset);
                            return;
                        }
                        UsingAny(instruction, Z80.WordRegister.PointerOrder(offset), pointerRegister =>
                        {
                            pointerRegister.LoadFromMemory(instruction, pointer, 0);
                            LoadIndirect(instruction, pointerRegister, offset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public override void Store(Instruction instruction, AssignableOperand destinationOperand)
        {
            switch (destinationOperand) {
                case VariableOperand destinationVariableOperand: {
                        var destinationVariable = destinationVariableOperand.Variable;
                        var destinationOffset = destinationVariableOperand.Offset;
                        if (destinationVariable.Register is WordRegister destinationRegister) {
                            Debug.Assert(destinationOffset == 0);
                            if (!Equals(destinationRegister, this)) {
                                destinationRegister.CopyFrom(instruction, this);
                            }
                            instruction.SetVariableRegister(destinationVariable, destinationOffset, destinationRegister);
                            return;
                        }
                        Store(instruction, destinationVariable, destinationOffset);
                        return;
                    }
                case IndirectOperand destinationIndirectOperand: {
                        var destinationPointer = destinationIndirectOperand.Variable;
                        var destinationOffset = destinationIndirectOperand.Offset;
                        if (destinationPointer.Register is WordRegister destinationPointerRegister) {
                            StoreIndirect(instruction,
                                 destinationPointerRegister, destinationOffset);
                            return;
                        }
                        UsingAny(instruction, Pointers(destinationOffset),
                            pointerRegister =>
                        {
                            StoreIndirect(instruction, pointerRegister, destinationOffset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }



        public override void StoreToMemory(Instruction instruction, string label)
        {
            //instruction.RemoveVariableRegisterId(Id);
            instruction.WriteLine("\tld\t(" + label + ")," + Name);
        }


        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            if (!IsPair()) {
                var candidates = PairRegisters.Where(r => r != pointerRegister).ToList();
                UsingAny(instruction, candidates, temporaryRegister =>
                {
                    temporaryRegister.LoadIndirect(instruction, pointerRegister, offset);
                    CopyFrom(instruction, temporaryRegister);
                });
                return;
            }
            Debug.Assert(Low != null && High != null);
            if (pointerRegister.IsIndex() && pointerRegister.IsOffsetInRange(offset + 1)) {
                Low.LoadIndirect(instruction, pointerRegister, offset);
                High.LoadIndirect(instruction, pointerRegister, offset + 1);
                return;
            }
            if (offset == 0) {
                if (Equals(pointerRegister, Hl)) {
                    if (Equals(this, Hl)) {
                        var candidates = ByteRegister.Registers.Where(r => !this.Contains(r)).ToList();
                        ByteOperation.UsingAnyRegister(instruction, candidates, byteRegister =>
                        {
                            byteRegister.LoadIndirect(instruction, pointerRegister, 0);
                            instruction.WriteLine("\tinc\t" + pointerRegister);
                            High.LoadIndirect(instruction, pointerRegister, 0);
                            if (!Equals(this, pointerRegister)) {
                                instruction.WriteLine("\tdec\t" + pointerRegister);
                            }
                            Low.CopyFrom(instruction, byteRegister);
                        });
                        return;
                    }
                    Low.LoadIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tinc\t" + pointerRegister);
                    High.LoadIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tdec\t" + pointerRegister);
                    return;
                }
                ByteRegister.UsingAccumulator(instruction, () =>
                {
                    ByteRegister.A.LoadIndirect(instruction, pointerRegister, 0);
                    Low.CopyFrom(instruction, ByteRegister.A);
                    instruction.WriteLine("\tinc\t" + pointerRegister);
                    ByteRegister.A.LoadIndirect(instruction, pointerRegister, 0);
                    High.CopyFrom(instruction, ByteRegister.A);
                    instruction.WriteLine("\tdec\t" + pointerRegister);
                });
                return;
            }
            instruction.BeginRegister(pointerRegister);
            pointerRegister.Add(instruction, offset);
            LoadIndirect(instruction, pointerRegister, 0);
            instruction.EndRegister(pointerRegister);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            if (!IsPair()) {
                var candidates = WordRegister.PairRegisters.Where(r => r != pointerRegister).ToList();
                WordRegister.UsingAny(instruction, candidates, temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, this);
                    temporaryRegister.StoreIndirect(instruction, pointerRegister, offset);
                });
                return;
            }
            Debug.Assert(Low != null && High != null);
            if (pointerRegister.IsIndex() && pointerRegister.IsOffsetInRange(offset + 1)) {
                Low.StoreIndirect(instruction, pointerRegister, offset);
                High.StoreIndirect(instruction, pointerRegister, offset + 1);
                return;
            }
            if (offset == 0) {
                if (Equals(pointerRegister, Hl)) {
                    Low.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tinc\t" + pointerRegister);
                    High.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tdec\t" + pointerRegister);
                    return;
                }
                ByteRegister.UsingAccumulator(instruction, () =>
                {
                    ByteRegister.A.CopyFrom(instruction, Low);
                    ByteRegister.A.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tinc\t" + pointerRegister);
                    ByteRegister.A.CopyFrom(instruction, High);
                    ByteRegister.A.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tdec\t" + pointerRegister);
                });
                return;
            }
            instruction.BeginRegister(pointerRegister);
            pointerRegister.Add(instruction, offset);
            StoreIndirect(instruction, pointerRegister, 0);
            instruction.EndRegister(pointerRegister);
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
                instruction.ChangedRegisters.Add(this);
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

        protected internal PairRegister(int id, ByteRegister highRegister, ByteRegister lowRegister) : base(id,
            highRegister.Name + lowRegister.Name)
        {
            Low = lowRegister;
            High = highRegister;
        }
    }

    internal class IndexRegister : WordRegister
    {
        public IndexRegister(int id, string name) : base(id, name)
        { }

        public override bool IsIndex() => true;

        public override bool IsPointer(int offset)
        {
            return offset >= -128 && offset <= 127;
        }
    }
}