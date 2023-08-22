﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.I8080
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public static readonly ByteRegister A = new ByteRegister(1, "a");
        public static readonly ByteRegister D = new ByteRegister(2, "d");
        public static readonly ByteRegister E = new ByteRegister(3, "e");
        public static readonly ByteRegister B = new ByteRegister(4, "b");
        public static readonly ByteRegister C = new ByteRegister(5, "c");
        public static readonly ByteRegister H = new ByteRegister(6, "h");
        public static readonly ByteRegister L = new ByteRegister(7, "l");

        public static List<Cate.ByteRegister> Registers = new List<Cate.ByteRegister> { A, D, E, B, C, H, L };
        public static List<Cate.ByteRegister> Accumulators => new List<Cate.ByteRegister> { A };

        protected ByteRegister(int id, string name) : base(id, name) { }

        public override Cate.WordRegister? PairRegister => WordRegister.Registers.FirstOrDefault(wordRegister => wordRegister.Contains(this));

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

        public override bool Matches(Register register)
        {
            switch (register) {
                case WordRegister wordRegister:
                    if (wordRegister.Contains(this))
                        return true;
                    break;
                    //case ByteRegister byteRegister:
                    //    if (Equals(byteRegister.PairRegister, PairRegister))
                    //        return true;
                    //    break;
            }
            return base.Matches(register);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(Equals(A));
            Instruction.WriteTabs(writer, tabCount);
            if (jump) {
                writer.WriteLine("\tsta\t" + Compiler.TemporaryByte + comment);
            }
            else {
                writer.WriteLine("\tpush\tpsw" + comment);
            }
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(Equals(A));
            Instruction.WriteTabs(writer, tabCount);
            if (jump) {
                writer.WriteLine("\tlda\t" + Compiler.TemporaryByte + comment);
            }
            else {
                writer.WriteLine("\tpop\tpsw" + comment);
            }
        }

        public override void Save(Instruction instruction)
        {
            if (Equals(A)) {
                instruction.WriteLine("\tpush\tpsw");
            }
            else {
                instruction.WriteLine("\tpush\t" + PairRegister);
            }
        }

        public override void Restore(Instruction instruction)
        {
            if (Equals(A)) {
                instruction.WriteLine("\tpop\tpsw");
            }
            else {
                instruction.WriteLine("\tpop\t" + PairRegister);
            }
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            if (Equals(this, A) && value == 0) {
                if (instruction.IsConstantAssigned(this, value)) {
                    instruction.AddChanged(this);
                    return;
                }
                instruction.WriteLine("\txra\ta");
                instruction.SetRegisterConstant(this, value);
                instruction.AddChanged(this);
                return;
            }
            base.LoadConstant(instruction, value);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tmvi\t" + Name + "," + value);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            var address = variable.MemoryAddress(offset);
            if (Equals(this, A)) {
                LoadFromMemory(instruction, address);
                return;
            }
            if (instruction.IsRegisterReserved(A) && !instruction.IsRegisterReserved(PointerRegister.Hl)) {
                using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                    PointerRegister.Hl.LoadConstant(instruction, address);
                    LoadIndirect(instruction, PointerRegister.Hl, 0);
                }
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.LoadFromMemory(instruction, address);
                CopyFrom(instruction, A);
            }
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            var address = variable.MemoryAddress(offset);
            if (Equals(this, A)) {
                instruction.WriteLine("\tsta\t" + address);
                instruction.SetVariableRegister(variable, offset, this);
                return;
            }
            if (!instruction.IsRegisterReserved(PointerRegister.Hl) && !PointerRegister.Hl.Contains(this)) {
                using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                    PointerRegister.Hl.LoadConstant(instruction, address);
                    StoreIndirect(instruction, PointerRegister.Hl, 0);
                }
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, this);
                A.StoreToMemory(instruction, address);
            }
        }

        public override void LoadIndirect(Instruction instruction, Variable pointer, int offset)
        {
            if (Conflicts(PointerRegister.Hl)) {
                var candidates = Registers.Where(r => !r.Conflicts(PointerRegister.Hl)).ToList();
                using var reservation = ByteOperation.ReserveAnyRegister(instruction, candidates);
                var register = reservation.ByteRegister;
                register.LoadIndirect(instruction, pointer, offset);
                CopyFrom(instruction, register);
                return;
            }

            if (offset == 0) {
                base.LoadIndirect(instruction, pointer, offset);
                return;
            }

            void ViaHl()
            {
                PointerRegister.Hl.TemporaryOffset(instruction, offset, () => { LoadIndirect(instruction, PointerRegister.Hl, 0); });
            }
            {
                var register = instruction.GetVariableRegister(pointer, 0);
                if (Equals(register, PointerRegister.Hl)) {
                    ViaHl();
                }
                else {
                    if (register is PointerRegister pointerRegister && Equals(this, A)) {
                        pointerRegister.TemporaryOffset(instruction, offset, () => { LoadIndirect(instruction, pointerRegister, 0); });
                        return;
                    }
                    using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                        if (register != null) {
                            PointerRegister.Hl.CopyFrom(instruction, (Cate.PointerRegister)register);
                        }
                        else {
                            PointerRegister.Hl.LoadFromMemory(instruction, pointer, 0);
                        }
                        ViaHl();
                    }
                }
            }
        }

        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    instruction.WriteLine("\tmov\t" + this + ",m");
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
                }
                if (Equals(this, A)) {
                    instruction.WriteLine("\tldax\t" + pointerRegister);
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
                }
                using (ByteOperation.ReserveRegister(instruction, A)) {
                    instruction.WriteLine("\tldax\t" + pointerRegister);
                    instruction.AddChanged(A);
                    instruction.RemoveRegisterAssignment(A);
                    CopyFrom(instruction, A);
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                }
                return;
            }
            if (pointerRegister.Matches(this)) {
                var candidates = Registers.Where(r => !r.Matches(pointerRegister)).ToList();
                using var reservation = ByteOperation.ReserveAnyRegister(instruction, candidates);
                var temporaryRegister = reservation.ByteRegister;
                temporaryRegister.LoadIndirect(instruction, pointerRegister, offset);
                CopyFrom(instruction, temporaryRegister);
                return;
            }
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                LoadIndirect(instruction, pointerRegister, 0);
            });
        }


        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            if (offset == 0) {
                if (Equals(pointerRegister, PointerRegister.Hl)) {
                    instruction.WriteLine("\tmov\tm," + this);
                    return;
                }
                if (Equals(this, A)) {
                    instruction.WriteLine("\tstax\t" + pointerRegister);
                    return;
                }
                using (ByteOperation.ReserveRegister(instruction, A)) {
                    A.CopyFrom(instruction, this);
                    instruction.WriteLine("\tstax\t" + pointerRegister);
                }
                return;
            }
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                StoreIndirect(instruction, pointerRegister, 0);
            });
        }

        public override void StoreIndirect(Instruction instruction, Variable pointer, int offset)
        {
            if (offset == 0) {
                base.StoreIndirect(instruction, pointer, offset);
                return;
            }

            var pointerRegister = instruction.GetVariableRegister(pointer, 0);
            if (Equals(pointerRegister, PointerRegister.Hl)) {
                StoreIndirect(instruction, PointerRegister.Hl, offset);
                return;
            }

            if (pointerRegister is PointerRegister wordRegister && Equals(this, A)) {
                StoreIndirect(instruction, wordRegister, offset);
                return;
            }
            using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                if (pointerRegister != null) {
                    PointerRegister.Hl.CopyFrom(instruction, (Cate.PointerRegister)pointerRegister);
                }
                else {
                    PointerRegister.Hl.LoadFromMemory(instruction, pointer, 0);
                }
                StoreIndirect(instruction, PointerRegister.Hl, offset);
            }
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            Debug.Assert(Equals(A));
            instruction.WriteLine("\tlda\t" + label);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            void StoreA()
            {
                instruction.WriteLine("\tsta\t" + label);
                instruction.RemoveRegisterAssignment(A);
            }
            if (Equals(A)) {
                StoreA();
            }
            else {
                using (ByteOperation.ReserveRegister(instruction, A)) {
                    A.CopyFrom(instruction, this);
                    StoreA();
                }
            }
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            if (Equals(sourceRegister, this)) return;

            instruction.WriteLine("\tmov\t" + this + "," + sourceRegister);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + Name);
            }
            instruction.AddChanged(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (Equals(this, A)) {
                switch (operand) {
                    case IntegerOperand integerOperand:
                        instruction.WriteLine("\t" + operation.Split('|')[1] + "\t" + integerOperand.IntegerValue);
                        instruction.RemoveRegisterAssignment(A);
                        return;
                    case StringOperand stringOperand:
                        instruction.WriteLine("\t" + operation.Split('|')[1] + "\t" + stringOperand.StringValue);
                        instruction.RemoveRegisterAssignment(A);
                        return;
                    case ByteRegisterOperand registerOperand: {
                            instruction.WriteLine("\t" + operation.Split('|')[0] + "\t" + registerOperand.Register);
                            return;
                        }
                    case VariableOperand variableOperand: {
                            var variable = variableOperand.Variable;
                            var offset = variableOperand.Offset;
                            var register = instruction.GetVariableRegister(variableOperand);
                            if (register is ByteRegister byteRegister) {
                                Debug.Assert(offset == 0);
                                instruction.WriteLine("\t" + operation.Split('|')[0] + "\t" + byteRegister);
                                return;
                            }

                            using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
                                WordRegister.Hl.LoadConstant(instruction, variable.MemoryAddress(offset));
                                instruction.WriteLine("\t" + operation.Split('|')[0] + "\tm");
                            }
                            return;
                        }
                    case IndirectOperand indirectOperand: {
                            var pointer = indirectOperand.Variable;
                            var offset = indirectOperand.Offset;
                            {
                                var register = instruction.GetVariableRegister(pointer, 0);
                                if (register is PointerRegister pointerRegister) {
                                    if (Equals(pointerRegister, PointerRegister.Hl)) {
                                        OperateIndirect(instruction, operation, offset);
                                    }
                                    else {
                                        using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                                            PointerRegister.Hl.CopyFrom(instruction, pointerRegister);
                                            OperateIndirect(instruction, operation, offset);
                                        }
                                    }
                                    return;
                                }
                            }
                            using (WordOperation.ReserveRegister(instruction, WordRegister.Hl)) {
                                WordRegister.Hl.LoadFromMemory(instruction, pointer, 0);
                                OperateIndirect(instruction, operation, offset);
                            }
                            return;
                        }
                }
                throw new NotImplementedException();
            }
            using (ByteOperation.ReserveRegister(instruction, A)) {
                A.CopyFrom(instruction, this);
                A.Operate(instruction, operation, change, operand);
                CopyFrom(instruction, A);
            }
        }

        private void OperateIndirect(Instruction instruction, string operation, int offset)
        {
            Debug.Assert(Equals(this, A));
            if (offset == 0) {
                instruction.WriteLine("\t" + operation.Split('|')[0] + "\tm");
                return;
            }
            PointerRegister.Hl.TemporaryOffset(instruction, offset, () =>
            {
                OperateIndirect(instruction, operation, 0);
            });
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            using (PointerOperation.ReserveRegister(instruction, PointerRegister.Hl)) {
                PointerRegister.Hl.LoadConstant(instruction, operand);
                instruction.WriteLine("\t" + operation.Split('|')[0] + "\tm");
            }
        }
    }
}
