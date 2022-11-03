using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Mc6809
{
    internal abstract class WordRegister : Cate.WordRegister
    {
        public static readonly List<Cate.WordRegister> Registers = new List<Cate.WordRegister>();
        public static readonly WordRegister D = new PairRegister(11, "d");
        public static readonly WordRegister X = new IndexRegister(12, "x");
        public static readonly WordRegister Y = new IndexRegister(13, "y");
        public static List<Cate.WordRegister> Pointers = new List<Cate.WordRegister>() { X, Y };
        public static List<Cate.WordRegister> PointerOrder = new List<Cate.WordRegister>() { X, Y, D };

        public static Cate.WordRegister FromId(int id)
        {
            return Registers.First(r => r.Id == id);
        }

        protected WordRegister(int id, string name) : base(id, name)
        {
            Registers.Add(this);
        }


        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            // save together
            throw new NotImplementedException();
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            // save together
            throw new NotImplementedException();
        }

        public override Cate.ByteRegister? High => Id == D.Id ? ByteRegister.A : base.Low;
        public override Cate.ByteRegister? Low => Id == D.Id ? ByteRegister.B : base.Low;
        public static List<Cate.WordRegister> IndexRegisters => new List<Cate.WordRegister>() { X, Y };

        public static string OffsetOperand(Cate.WordRegister register, int offset)
        {
            return offset != 0 ? offset + "," + register : "," + register;
        }

        public override bool IsOffsetInRange(int offset) => true;

        public override bool IsPointer(int offset) => IsIndex();


        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tld" + this + "\t#" + value);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tld" + this + "\t" + variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
            instruction.ChangedRegisters.Add(this);
        }

        public void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            var destinationAddress = variable.MemoryAddress(offset);
            instruction.WriteLine("\tst" + this + "\t" + destinationAddress);
            instruction.SetVariableRegister(variable, offset, this);
        }


        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tld" + this + "\t" + label);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tst" + this + "\t" + label);
        }

        private void LoadIndirect(Instruction instruction, Variable pointer, int offset)
        {
            if (pointer.Register == null && offset == 0) {
                instruction.WriteLine("\tld" + this + "\t[" + pointer.MemoryAddress(0) + "]");
                instruction.ChangedRegisters.Add(this);
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            WordOperation.UsingAnyRegister(instruction, Pointers, pointerRegister =>
            {
                pointerRegister.LoadFromMemory(instruction, pointer, 0);
                LoadIndirect(instruction, pointerRegister, offset);
            });
        }

        private void StoreIndirect(Instruction instruction, Variable pointer, int offset)
        {
            if (pointer.Register == null && offset == 0) {
                instruction.WriteLine("\tst" + this + "\t[" + pointer.MemoryAddress(0) + "]");
                return;
            }
            WordOperation.UsingAnyRegister(instruction, Pointers, pointerRegister =>
            {
                pointerRegister.LoadFromMemory(instruction, pointer, 0);
                StoreIndirect(instruction, pointerRegister, offset);
            });
        }


        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            instruction.WriteLine("\tld" + this + "\t" + OffsetOperand(pointerRegister, offset));
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            instruction.WriteLine("\tst" + this + "\t" + OffsetOperand(pointerRegister, offset));
        }


        public override void Load(Instruction instruction, Operand sourceOperand)
        {
            switch (sourceOperand) {
                case IntegerOperand sourceIntegerOperand:
                    LoadConstant(instruction, sourceIntegerOperand.IntegerValue);
                    return;
                case PointerOperand sourcePointerOperand:
                    LoadConstant(instruction, sourcePointerOperand.MemoryAddress());
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
                        LoadIndirect(instruction, pointer, offset);
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
                            instruction.RemoveRegisterAssignment(this);
                            return;
                        }
                        StoreToMemory(instruction, destinationVariable, destinationOffset);
                        return;
                    }
                case IndirectOperand destinationIndirectOperand: {
                        var pointer = destinationIndirectOperand.Variable;
                        var offset = destinationIndirectOperand.Offset;
                        if (pointer.Register is WordRegister destinationPointerRegister) {
                            StoreIndirect(instruction,
                                 destinationPointerRegister, offset);
                            return;
                        }
                        StoreIndirect(instruction, pointer, offset);
                        return;
                    }
            }
            throw new NotImplementedException();
        }



        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            if (Equals(sourceRegister, this)) return;
            instruction.WriteLine("\ttfr\t" + sourceRegister + "," + this);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            var register = operand.Register;
            if (register is WordRegister rightRegister) {
                instruction.WriteLine("\tst" + rightRegister + "\t" + DirectPage.Word);
                instruction.WriteLine("\t" + operation + Name + "\t" + DirectPage.Word);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            }
            Mc6809.WordOperation.Operate(instruction, operation + Name, change, operand, 1);
        }


        //public static void Using(Instruction instruction, List<Cate.WordRegister> candidates,
        //    Action<Cate.WordRegister> action)
        //{
        //    var temporaryRegister = TemporaryRegister(instruction, candidates);
        //    instruction.BeginRegister(temporaryRegister);
        //    action(temporaryRegister);
        //    instruction.EndRegister(temporaryRegister);
        //}

        //public static void Using(Instruction instruction, Cate.WordRegister register, Action action)
        //{
        //    if (instruction.IsRegisterInUse(register)) {
        //        register.Save(instruction);
        //        action();
        //        register.Restore(instruction);
        //    }
        //    else {
        //        instruction.BeginRegister(register);
        //        action();
        //        instruction.EndRegister(register);
        //    }
        //}

        public override bool Conflicts(Cate.Register? register)
        {
            if (register is ByteRegister byteRegister && Contains(byteRegister))
                return true;
            return base.Conflicts(register);
        }

        public void Operate(Instruction instruction, string operation, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + Name);
            }
        }
    }


    internal class PairRegister : WordRegister
    {
        public PairRegister(int id, string name) : base(id, name)
        { }

        public override Cate.ByteRegister? High => ByteRegister.A;
        public override bool IsPair() => true;

        //public override bool Contains(Cate.ByteRegister register)
        //{
        //    return base.Contains(register) || register.Id == ByteRegister.A.Id || register.Id == ByteRegister.B.Id;
        //}


        public override void Add(Instruction instruction, int offset)
        {
            instruction.WriteLine("\taddd\t#" + offset);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpshs\ta,b");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpuls\ta,b");
        }

        public override Cate.ByteRegister? Low => ByteRegister.B;

        public override bool Matches(Register register)
        {
            return Conflicts(register) || Equals(register, Low) || Equals(register, High);
        }

        public virtual bool Contains(ByteRegister byteRegister)
        {
            return base.Contains(byteRegister) || Equals(byteRegister, ByteRegister.A) || Equals(byteRegister, ByteRegister.B);
        }
    }

    internal class IndexRegister : WordRegister
    {
        public IndexRegister(int id, string name) : base(id, name)
        { }

        public override void Add(Instruction instruction, int offset)
        {
            instruction.WriteLine("\tlea" + Name + "\t" + OffsetOperand(this, offset));
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override bool IsIndex() => true;

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpshs\t" + Name);
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpuls\t" + Name);
        }
    }
}