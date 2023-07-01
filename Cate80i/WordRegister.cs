﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.I8080
{
    internal class WordRegister : Cate.WordRegister
    {
        public static readonly WordRegister Hl = new WordRegister(11, ByteRegister.H, ByteRegister.L, true);
        public static readonly WordRegister De = new WordRegister(12, ByteRegister.D, ByteRegister.E, false);
        public static readonly WordRegister Bc = new WordRegister(13, ByteRegister.B, ByteRegister.C, false);

        public static readonly List<Cate.WordRegister> Registers = new List<Cate.WordRegister>() { Hl, De, Bc };

        public override Cate.ByteRegister? Low { get; }
        public override Cate.ByteRegister? High { get; }

        public IEnumerable<Register> ByteRegisters
        {
            get
            {
                Debug.Assert(Low != null);
                Debug.Assert(High != null);
                return new Register[] { Low, High };
            }
        }

        public override bool Matches(Register register)
        {
            return Conflicts(register) || register is ByteRegister byteRegister && Contains(byteRegister);
        }

        public readonly bool Addable;

        public WordRegister(int id, ByteRegister high, ByteRegister low, bool addable) : base(id, high.Name)
        {
            High = high;
            Low = low;
            Addable = addable;
        }

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

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpush\t" + Name);
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpop\t" + Name);
        }

        //public override void Add(Instruction instruction, int offset)
        //{
        //    if (offset == 0) {
        //        return;
        //    }

        //    const int threshold = 4;
        //    var count = offset & 0xffff;
        //    if (count <= threshold) {
        //        Loop("inx");
        //        instruction.AddChanged(this);
        //        instruction.RemoveRegisterAssignment(this);
        //        return;
        //    }

        //    if (count >= 0x10000 - threshold) {
        //        count = 0x10000 - count;
        //        Loop("dcx");
        //        instruction.AddChanged(this);
        //        instruction.RemoveRegisterAssignment(this);
        //        return;
        //    }

        //    void Loop(string operation)
        //    {
        //        for (var i = 0; i < count; ++i) {
        //            instruction.WriteLine("\t" + operation + "\t" + Name);
        //        }
        //    }

        //    if (Equals(this, Hl)) {
        //        void ViaRegister(Cate.WordRegister temporaryRegister)
        //        {
        //            temporaryRegister.LoadConstant(instruction, offset);
        //            instruction.WriteLine("\tdad\t" + temporaryRegister);
        //        }

        //        var candidates = new List<Cate.WordRegister>() { De, Bc };
        //        if (candidates.Any(r => !instruction.IsRegisterReserved(r))) {
        //            using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
        //            ViaRegister(reservation.WordRegister);
        //        }
        //        else {
        //            instruction.WriteLine("\tpush\t" + De);
        //            ViaRegister(De);
        //            instruction.WriteLine("\tpop\t" + De);
        //        }
        //    }
        //    else {
        //        Debug.Assert(IsPair());
        //        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
        //            Debug.Assert(Low != null && High != null);
        //            ByteRegister.A.CopyFrom(instruction, Low);
        //            instruction.WriteLine("\tadi\tlow " + offset);
        //            Low.CopyFrom(instruction, ByteRegister.A);
        //            ByteRegister.A.CopyFrom(instruction, High);
        //            instruction.WriteLine("\taci\thigh " + offset);
        //            High.CopyFrom(instruction, ByteRegister.A);
        //        }
        //    }

        //    instruction.AddChanged(this);
        //    instruction.RemoveRegisterAssignment(this);
        //}

        //public override bool IsAddable() => Equals(this, Hl);

        //public override bool IsOffsetInRange(int offset)
        //{
        //    return offset == 0;
        //}

        //public override bool IsPointer(int offset)
        //{
        //    return offset == 0;
        //}

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tlxi\t" + Name + "," + value);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            if (Equals(this, Hl)) {
                instruction.WriteLine("\tlhld\t" + label);
            }
            else {
                using (WordOperation.ReserveRegister(instruction, Hl)) {
                    Hl.LoadFromMemory(instruction, label);
                    CopyFrom(instruction, Hl);
                }
            }

            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            if (Equals(this, Hl)) {
                instruction.WriteLine("\tshld\t" + label);
            }
            else {
                using (WordOperation.ReserveRegister(instruction, Hl)) {
                    Hl.CopyFrom(instruction, this);
                    Hl.StoreToMemory(instruction, label);
                }
            }
        }

        //public override void Load(Instruction instruction, Operand sourceOperand)
        //{
        //    switch (sourceOperand) {
        //        case IntegerOperand sourceIntegerOperand:
        //            var value = sourceIntegerOperand.IntegerValue;
        //            if (instruction.IsConstantAssigned(this, value)) return;
        //            LoadConstant(instruction, value.ToString());
        //            instruction.SetRegisterConstant(this, value);
        //            return;
        //        case PointerOperand sourcePointerOperand:
        //            if (instruction.IsConstantAssigned(this, sourcePointerOperand)) return;
        //            instruction.WriteLine("\tlxi\t" + this + "," + sourcePointerOperand.MemoryAddress());
        //            instruction.SetRegisterConstant(this, sourcePointerOperand);
        //            instruction.AddChanged(this);
        //            return;
        //        case VariableOperand sourceVariableOperand: {
        //                var sourceVariable = sourceVariableOperand.Variable;
        //                var sourceOffset = sourceVariableOperand.Offset;
        //                var variableRegister = instruction.GetVariableRegister(sourceVariableOperand);
        //                if (variableRegister is WordRegister sourceRegister) {
        //                    Debug.Assert(sourceOffset == 0);
        //                    if (!Equals(sourceRegister, this)) {
        //                        CopyFrom(instruction, sourceRegister);
        //                        instruction.CancelOperandRegister(sourceVariableOperand);
        //                    }
        //                    return;
        //                }
        //                LoadFromMemory(instruction, sourceVariable, sourceOffset);
        //                instruction.CancelOperandRegister(sourceVariableOperand);
        //                return;
        //            }
        //        case IndirectOperand sourceIndirectOperand: {
        //                var pointer = sourceIndirectOperand.Variable;
        //                var offset = sourceIndirectOperand.Offset;
        //                var pointerRegister = instruction.GetVariableRegister(pointer, 0);
        //                if (pointerRegister is WordRegister pointerWordRegister && !Equals(pointerWordRegister, this)) {
        //                    LoadIndirect(instruction, pointerWordRegister, offset);
        //                    instruction.CancelOperandRegister(sourceIndirectOperand);
        //                    return;
        //                }

        //                if (Equals(this, Hl)) {
        //                    using var reservation = WordOperation.ReserveAnyRegister(instruction, new List<Cate.WordRegister> { Bc, De });
        //                    var temporaryRegister = reservation.WordRegister;
        //                    Hl.LoadFromMemory(instruction, pointer, 0);
        //                    temporaryRegister.LoadIndirect(instruction, Hl, offset);
        //                    Hl.CopyFrom(instruction, temporaryRegister);
        //                    instruction.CancelOperandRegister(sourceIndirectOperand);
        //                    return;
        //                }
        //                using (WordOperation.ReserveRegister(instruction, Hl)) {
        //                    Hl.LoadFromMemory(instruction, pointer, 0);
        //                    LoadIndirect(instruction, Hl, offset);
        //                    instruction.CancelOperandRegister(sourceIndirectOperand);
        //                }
        //                return;
        //            }
        //    }

        //    throw new NotImplementedException();
        //}

        public override void Store(Instruction instruction, AssignableOperand destinationOperand)
        {
            switch (destinationOperand) {
                case VariableOperand destinationVariableOperand: {
                        var variable = destinationVariableOperand.Variable;
                        var offset = destinationVariableOperand.Offset;
                        if (variable.Register is WordRegister destinationRegister) {
                            Debug.Assert(offset == 0);
                            if (!Equals(destinationRegister, this)) {
                                destinationRegister.CopyFrom(instruction, this);
                            }

                            instruction.SetVariableRegister(variable, offset, destinationRegister);
                            return;
                        }

                        StoreToMemory(instruction, variable.MemoryAddress(offset));
                        instruction.SetVariableRegister(variable, offset, this);
                        return;
                    }
                case IndirectOperand destinationIndirectOperand: {
                        var pointer = destinationIndirectOperand.Variable;
                        var offset = destinationIndirectOperand.Offset;
                        var register = instruction.GetVariableRegister(pointer, 0);
                        if (register is PointerRegister pointerRegister) {
                            StoreIndirect(instruction, pointerRegister, offset);
                            return;
                        }

                        if (Equals(this, Hl)) {
                            var candidates = Registers.Where(r => !Equals(r, Hl)).ToList();
                            using var reservation = WordOperation.ReserveAnyRegister(instruction, candidates);
                            var temporaryRegister = reservation.WordRegister;
                            temporaryRegister.CopyFrom(instruction, this);
                            using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                                PointerRegister.Hl.LoadFromMemory(instruction, pointer, 0);
                                temporaryRegister.StoreIndirect(instruction, PointerRegister.Hl, offset);
                            }
                            return;
                        }
                        using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                            PointerRegister.Hl.LoadFromMemory(instruction, pointer, 0);
                            StoreIndirect(instruction, PointerRegister.Hl, offset);
                        }
                        return;
                    }
            }

            throw new NotImplementedException();
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            if (Equals(this, Hl)) {
                instruction.WriteLine("\tlhld\t" + variable.MemoryAddress(offset));
            }
            else {
                using (WordOperation.ReserveRegister(instruction, Hl)) {
                    Hl.LoadFromMemory(instruction, variable, offset);
                    CopyFrom(instruction, Hl);
                }
            }
            instruction.SetVariableRegister(variable, offset, this);
            instruction.AddChanged(this);
        }

        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            Debug.Assert(Low != null && High != null);
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    if (Equals(this, Hl)) {
                        using var reservation =
                            WordOperation.ReserveAnyRegister(instruction, new List<Cate.WordRegister> { Bc, De });
                        var wordRegister = reservation.WordRegister;
                        wordRegister.LoadIndirect(instruction, pointerRegister, offset);
                        CopyFrom(instruction, wordRegister);
                        return;
                    }

                    Low.LoadIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tinx\t" + pointerRegister);
                    High.LoadIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tdcx\t" + pointerRegister);
                    return;
                }
                using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                    instruction.WriteLine("\tldax\t" + pointerRegister);
                    Low.CopyFrom(instruction, ByteRegister.A);
                    instruction.WriteLine("\tinx\t" + pointerRegister);
                    instruction.WriteLine("\tldax\t" + pointerRegister);
                    High.CopyFrom(instruction, ByteRegister.A);
                    instruction.WriteLine("\tdcx\t" + pointerRegister);
                    instruction.AddChanged(ByteRegister.A);
                    instruction.RemoveRegisterAssignment(ByteRegister.A);
                }
                return;
            }
            if (Math.Abs(offset) > 1) {
                var changed = instruction.IsChanged(pointerRegister);
                pointerRegister.Save(instruction);
                pointerRegister.Add(instruction, offset);
                LoadIndirect(instruction, pointerRegister, 0);
                pointerRegister.Restore(instruction);
                if (!changed) {
                    instruction.RemoveChanged(pointerRegister);
                }
                return;
            }
            pointerRegister.Add(instruction, offset);
            LoadIndirect(instruction, pointerRegister, 0);
            if (!Equals(pointerRegister.WordRegister, this)) {
                pointerRegister.Add(instruction, -offset);
            }
        }

        public override void LoadIndirect(Instruction instruction, Variable pointer, int offset)
        {
            if (Equals(this, Hl)) {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, new List<Cate.WordRegister> { Bc, De });
                var temporaryRegister = reservation.WordRegister;
                Hl.LoadFromMemory(instruction, pointer, 0);
                temporaryRegister.LoadIndirect(instruction, PointerRegister.Hl, offset);
                Hl.CopyFrom(instruction, temporaryRegister);
                return;
            }
            base.LoadIndirect(instruction, pointer, offset);
        }


        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            Debug.Assert(Low != null && High != null);
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    if (Equals(this, Hl)) {
                        using var reservation = WordOperation.ReserveAnyRegister(instruction, new List<Cate.WordRegister> { Bc, De });
                        var wordRegister = reservation.WordRegister;
                        wordRegister.CopyFrom(instruction, this);
                        wordRegister.StoreIndirect(instruction, pointerRegister, offset);
                        return;
                    }

                    Low.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tinx\t" + pointerRegister);
                    High.StoreIndirect(instruction, pointerRegister, 0);
                    instruction.WriteLine("\tdcx\t" + pointerRegister);
                    return;
                }
                using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                    ByteRegister.A.CopyFrom(instruction, Low);
                    instruction.WriteLine("\tstax\t" + pointerRegister);
                    instruction.WriteLine("\tinx\t" + pointerRegister);
                    ByteRegister.A.CopyFrom(instruction, High);
                    instruction.WriteLine("\tstax\t" + pointerRegister);
                    instruction.WriteLine("\tdcx\t" + pointerRegister);
                }
                return;
            }

            void AddAndStore(Cate.PointerRegister wordRegister)
            {
                wordRegister.Add(instruction, offset);
                StoreIndirect(instruction, wordRegister, 0);
            }

            if (!instruction.TemporaryRegisters.Contains(pointerRegister)) {
                AddAndStore(pointerRegister);
            }
            else {
                var candidates = PointerRegister.Registers.Where(r => !Equals(r, pointerRegister)).ToList();
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
                var temporaryRegister = reservation.PointerRegister;
                temporaryRegister.CopyFrom(instruction, pointerRegister);
                AddAndStore(temporaryRegister);
            }
        }

        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            if (Equals(this, sourceRegister))
                return;

            Debug.Assert(Low != null && High != null);
            Debug.Assert(sourceRegister.Low != null && sourceRegister.High != null);
            Low.CopyFrom(instruction, sourceRegister.Low);
            High.CopyFrom(instruction, sourceRegister.High);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new NotImplementedException();
        }
    }
}
