using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.MuCom87
{
    class WordRegister : Cate.WordRegister
    {
        public static readonly WordRegister Hl = new WordRegister(11, ByteRegister.H, ByteRegister.L);
        public static readonly WordRegister De = new WordRegister(12, ByteRegister.D, ByteRegister.E);
        public static readonly WordRegister Bc = new WordRegister(13, ByteRegister.B, ByteRegister.C);

        public static List<Cate.WordRegister> Registers = new List<Cate.WordRegister>() { Hl, De, Bc };

        public override Cate.ByteRegister? Low { get; }
        public override Cate.ByteRegister? High { get; }

        public string LowName
        {
            get
            {
                Debug.Assert(Low != null);
                return Low.Name;
            }
        }
        public string HighName
        {
            get
            {
                Debug.Assert(High != null);
                return High.Name;
            }
        }

        public IEnumerable<Register> ByteRegisters
        {
            get
            {
                Debug.Assert(Low != null);
                Debug.Assert(High != null);
                return new Register[] { Low, High };
            }
        }

        public WordRegister(int id, ByteRegister high, ByteRegister low) : base(id, high.Name + low.Name)
        {
            High = high;
            Low = low;
        }


        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpush\t" + HighName + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpop\t" + HighName + comment);
        }

        public override bool Conflicts(Register? register)
        {
            if (register is ByteRegister byteRegister && Contains(byteRegister)) {
                return true;
            }
            return base.Conflicts(register);
        }

        public override void Add(Instruction instruction, int offset)
        {
            if (offset == 0) { return; }

            if (offset > 0) {
                if (offset < 8) {
                    var count = offset;
                    while (count > 0) {
                        instruction.WriteLine("\tinx\t" + HighName);
                        --count;
                    }
                    instruction.RemoveRegisterAssignment(this);
                    instruction.ChangedRegisters.Add(this);
                    return;
                }
                ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
                {
                    Debug.Assert(Low != null);
                    Debug.Assert(High != null);
                    ByteRegister.A.CopyFrom(instruction, Low);
                    instruction.WriteLine("\tadi\ta,low " + offset);
                    Low.CopyFrom(instruction, ByteRegister.A);
                    ByteRegister.A.CopyFrom(instruction, High);
                    instruction.WriteLine("\taci\ta,high " + offset);
                    High.CopyFrom(instruction, ByteRegister.A);
                });
            }
            else {
                if (offset > -8) {
                    var count = -offset;
                    while (count > 0) {
                        instruction.WriteLine("\tdcx\t" + HighName);
                        --count;
                    }
                    return;
                }
                ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
                {
                    Debug.Assert(Low != null);
                    Debug.Assert(High != null);
                    ByteRegister.A.CopyFrom(instruction, Low);
                    instruction.WriteLine("\tsui\ta,low " + -offset);
                    Low.CopyFrom(instruction, ByteRegister.A);
                    ByteRegister.A.CopyFrom(instruction, High);
                    instruction.WriteLine("\tsbi\ta,high " + -offset);
                    High.CopyFrom(instruction, ByteRegister.A);
                });
            }
        }

        public override bool IsOffsetInRange(int offset)
        {
            return offset == 0;
        }

        public override bool IsPointer(int offset)
        {
            return offset == 0;
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tlxi\t" + HighName + "," + value);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tl" + Name + "d\t" + label);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\ts" + Name + "d\t" + label);
        }

        public override void Load(Instruction instruction, Operand operand)
        {
            switch (operand) {
                case IntegerOperand sourceIntegerOperand:
                    var value = sourceIntegerOperand.IntegerValue;
                    LoadConstant(instruction, value.ToString());
                    return;
                case PointerOperand sourcePointerOperand:
                    LoadConstant(instruction, sourcePointerOperand.MemoryAddress());
                    return;
                case VariableOperand sourceVariableOperand: {
                        var sourceVariable = sourceVariableOperand.Variable;
                        var sourceOffset = sourceVariableOperand.Offset;
                        if (sourceVariable.Register is Cate.WordRegister sourceRegister) {
                            Debug.Assert(sourceOffset == 0);
                            if (!Equals(sourceRegister, this)) {
                                CopyFrom(instruction, sourceRegister);
                                instruction.ChangedRegisters.Add(this);
                                instruction.RemoveRegisterAssignment(this);
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
                            if (!Equals(pointerRegister, this)) {
                                LoadIndirect(instruction, pointerRegister, offset);
                            }
                            else {
                                WordOperation.UsingAnyRegister(instruction, WordRegister.Registers, temporaryRegister =>
                                {
                                    temporaryRegister.CopyFrom(instruction, pointerRegister);
                                    LoadIndirect(instruction, temporaryRegister, offset);
                                });
                            }
                            return;
                        }
                        WordOperation.UsingAnyRegister(instruction, WordRegister.Registers, temporaryRegister =>
                        {
                            temporaryRegister.LoadFromMemory(instruction, pointer, 0);
                            LoadIndirect(instruction, temporaryRegister, offset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public override void Store(Instruction instruction, AssignableOperand operand)
        {
            switch (operand) {
                case VariableOperand destinationVariableOperand: {
                        var destinationVariable = destinationVariableOperand.Variable;
                        var destinationOffset = destinationVariableOperand.Offset;
                        if (destinationVariable.Register is Cate.WordRegister destinationRegister) {
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
                        if (destinationPointer.Register is Cate.WordRegister destinationPointerRegister) {
                            StoreIndirect(instruction, destinationPointerRegister, destinationOffset);
                            return;
                        }
                        WordOperation.UsingAnyRegister(instruction, WordRegister.Registers,
                            pointerRegister =>
                        {
                            pointerRegister.LoadFromMemory(instruction, destinationPointer, 0);
                            StoreIndirect(instruction, pointerRegister, destinationOffset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tl" + Name + "d\t" + variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
            instruction.ChangedRegisters.Add(this);
            instruction.SetRegisterOffset(this, offset);
        }

        private void Store(Instruction instruction, Variable variable, int offset)
        {
            var destinationAddress = variable.MemoryAddress(offset);
            instruction.WriteLine("\ts" + Name + "d\t" + destinationAddress);
            instruction.SetVariableRegister(variable, offset, this);
        }



        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            switch (pointerRegister) {
                case WordRegister wordRegister when offset == 0:
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

        private void LoadIndirect(Instruction instruction, WordRegister wordRegister)
        {
            if (Equals(wordRegister, this)) {
                WordOperation.UsingAnyRegister(instruction, WordRegister.RegistersOtherThan(this), temporaryRegister =>
                {
                    var register = ((WordRegister)temporaryRegister);
                    register.LoadIndirect(instruction, wordRegister);
                    instruction.ChangedRegisters.Add(register);
                    CopyFrom(instruction, temporaryRegister);
                });
                return;
            }
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                Debug.Assert(Low != null);
                Debug.Assert(High != null);
                ByteRegister.A.LoadIndirect(instruction, wordRegister);
                Low.CopyFrom(instruction, ByteRegister.A);
                instruction.WriteLine("\tinx\t" + wordRegister.HighName);
                ByteRegister.A.LoadIndirect(instruction, wordRegister);
                High.CopyFrom(instruction, ByteRegister.A);
                instruction.WriteLine("\tdcx\t" + wordRegister.HighName);
            });
            instruction.ChangedRegisters.Add(this);
        }

        private static List<Cate.WordRegister> RegistersOtherThan(WordRegister register)
        {
            return Registers.FindAll(r => !Equals(r, register));
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            switch (pointerRegister) {
                case WordRegister wordRegister when offset == 0:
                    StoreIndirect(instruction, wordRegister);
                    return;
                case WordRegister wordRegister:
                    wordRegister.TemporaryOffset(instruction, offset, () =>
                    {
                        StoreIndirect(instruction, wordRegister);
                    });
                    return;
            }
            throw new NotImplementedException();
        }

        protected virtual void StoreIndirect(Instruction instruction, WordRegister wordRegister)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                Debug.Assert(Low != null);
                Debug.Assert(High != null);
                ByteRegister.A.CopyFrom(instruction, Low);
                ByteRegister.A.StoreIndirect(instruction, wordRegister);
                instruction.WriteLine("\tinx\t" + wordRegister.HighName);
                ByteRegister.A.CopyFrom(instruction, High);
                ByteRegister.A.StoreIndirect(instruction, wordRegister);
                instruction.WriteLine("\tdcx\t" + wordRegister.HighName);
                instruction.ChangedRegisters.Add(ByteRegister.A);
            });
        }

        public override void CopyFrom(Instruction instruction, Cate.WordRegister register)
        {
            ByteOperation.UsingRegister(instruction, ByteRegister.A, () =>
            {
                Debug.Assert(register.Low != null);
                ByteRegister.A.CopyFrom(instruction, register.Low);
                Debug.Assert(Low != null);
                Low.CopyFrom(instruction, ByteRegister.A);
                Debug.Assert(register.High != null);
                ByteRegister.A.CopyFrom(instruction, register.High);
                Debug.Assert(High != null);
                High.CopyFrom(instruction, ByteRegister.A);
            });
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
            //if (instruction.SourceRegisterCount(this) > 1) {
            var changed = instruction.ChangedRegisters.Contains(this);
            Save(instruction);
            Add(instruction, offset);
            action();
            Restore(instruction);
            if (!changed) {
                instruction.ChangedRegisters.Remove(this);
            }
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpush\t" + HighName);
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpop\t" + HighName);
        }

        protected virtual bool IsOffsetShort(int offset) => Math.Abs(offset) <= 4;
    }
}
