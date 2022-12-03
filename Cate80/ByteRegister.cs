using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public static List<Cate.ByteRegister> Registers = new List<Cate.ByteRegister>();


        public static Cate.ByteRegister FromId(int id)
        {
            var register = Registers.Find(r => r.Id == id);
            return register ?? throw new ArgumentOutOfRangeException();
        }

        public static Cate.ByteRegister FromName(string name)
        {
            var register = Registers.Find(r => r.Name == name);
            return register ?? throw new ArgumentOutOfRangeException();
        }

        private ByteRegister(int id, string name) : base(id, name)
        {
            Registers.Add(this);
        }

        public override Cate.WordRegister? PairRegister => WordRegister.Registers.FirstOrDefault(wordRegister => wordRegister.Name.Contains(Name));


        public static readonly ByteRegister A = new ByteRegister(1, "a");
        public static readonly ByteRegister D = new ByteRegister(2, "d");
        public static readonly ByteRegister E = new ByteRegister(3, "e");
        public static readonly ByteRegister B = new ByteRegister(4, "b");
        public static readonly ByteRegister C = new ByteRegister(5, "c");
        public static readonly ByteRegister H = new ByteRegister(6, "h");
        public static readonly ByteRegister L = new ByteRegister(7, "l");

        public static List<Cate.ByteRegister> Accumulators => new List<Cate.ByteRegister>() { A };
        //public static readonly List<ByteRegister> LowRegisters = new List<ByteRegister>() { C, E, L };

        //public override bool IsLow() => LowRegisters.Contains(this);


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
            }
            return base.Matches(register);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(Equals(A));
            Instruction.WriteTabs(writer, tabCount);
            if (jump) {
                writer.WriteLine("\tld\t(@Temporary@Byte),a" + comment);
            }
            else {
                writer.WriteLine("\tpush\taf" + comment);
            }
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Debug.Assert(Equals(A));
            Instruction.WriteTabs(writer, tabCount);
            if (jump) {
                writer.WriteLine("\tld\ta,(@Temporary@Byte)" + comment);
            }
            else {
                writer.WriteLine("\tpop\taf" + comment);
            }
        }

        public static void UsingAccumulator(Instruction instruction, Action action)
        {
            Using(instruction, A, action);
        }

        public static void UsingAny(Instruction instruction, List<Cate.ByteRegister> candidates,
            Action<Cate.ByteRegister> action)
        {
            var temporaryRegister = TemporaryRegister(instruction, candidates);
            instruction.BeginRegister(temporaryRegister);
            action(temporaryRegister);
            instruction.EndRegister(temporaryRegister);
        }

        //public static void UsingAny(Instruction instruction, Action<Cate.ByteRegister> action)
        //{
        //    UsingAny(instruction, Registers, action);
        //}


        public static Cate.ByteRegister TemporaryRegister(Instruction instruction, IEnumerable<Cate.ByteRegister> candidates)
        {
            var register = candidates.First(r => !instruction.IsRegisterInUse(r));
            Debug.Assert(register != null);
            return register;
        }

        public static void Using(Instruction instruction, Cate.ByteRegister register, Action action)
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
                    instruction.ChangedRegisters.Add(otherRegister);
                    action();
                    register.CopyFrom(instruction, otherRegister);
                });
                return;
            }
            InvokeAction();
        }

        public static void Using(Instruction instruction, ByteRegister register, Operand operand, Action action)
        {
            if (Equals(operand.Register, register)) {
                instruction.BeginRegister(operand.Register);
                action();
                instruction.EndRegister(operand.Register);
                return;
            }
            Using(instruction, register, action);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tld\t" + Name + "," + value);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            if (value == 0 && Equals(this, A)) {
                instruction.WriteLine("\txor\ta");
                instruction.ChangedRegisters.Add(this);
                instruction.RemoveRegisterAssignment(this);
                return;
            }
            base.LoadConstant(instruction, value);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            Debug.Assert(Equals(A));
            instruction.WriteLine("\tld\ta,(" + label + ")");
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            Debug.Assert(Equals(A));
            instruction.WriteLine("\tld\t(" + label + "),a");
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            var address = variable.MemoryAddress(offset);
            if (Equals(this, A)) {
                LoadFromMemory(instruction, address);
                return;
            }
            if (instruction.IsRegisterInUse(A) && !instruction.IsRegisterInUse(WordRegister.Hl)) {
                instruction.BeginRegister(WordRegister.Hl);
                WordRegister.Hl.LoadConstant(instruction, address);
                LoadIndirect(instruction, WordRegister.Hl, 0);
                instruction.EndRegister(WordRegister.Hl);
                return;
            }
            UsingAccumulator(instruction, () =>
            {
                A.LoadFromMemory(instruction, address);
                CopyFrom(instruction, A);
            });
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            var address = variable.MemoryAddress(offset);
            if (Equals(this, A)) {
                instruction.WriteLine("\tld\t(" + address + "),a");
                instruction.SetVariableRegister(variable, offset, this);
                return;
            }
            if (!instruction.IsRegisterInUse(WordRegister.Hl) && !WordRegister.Hl.Contains(this)) {
                instruction.BeginRegister(WordRegister.Hl);
                WordRegister.Hl.LoadConstant(instruction, address);
                StoreIndirect(instruction, WordRegister.Hl, 0);
                instruction.EndRegister(WordRegister.Hl);
                return;
            }
            UsingAccumulator(instruction, () =>
            {
                A.CopyFrom(instruction, this);
                A.StoreToMemory(instruction, address);
            });
        }

        public override void Load(Instruction instruction, Operand sourceOperand)
        {
            switch (sourceOperand) {
                case IndirectOperand sourceIndirectOperand: {
                        var pointer = sourceIndirectOperand.Variable;
                        var offset = sourceIndirectOperand.Offset;
                        var register = instruction.GetVariableRegister(pointer, 0);
                        {
                            if (register is WordRegister pointerRegister) {
                                LoadIndirect(instruction, pointerRegister, offset);
                                return;
                            }
                        }
                        WordRegister.UsingAny(instruction, Z80.WordRegister.Pointers(offset),
                            pointerRegister =>
                            {
                                //var sourceString = pointer.MemoryAddress(0);
                                //instruction.WriteLine("\tld\t" + pointerRegister + ",(" + sourceString + ")");
                                pointerRegister.LoadFromMemory(instruction, pointer, 0);
                                LoadIndirect(instruction, pointerRegister, offset);
                            });
                        return;
                    }
            }
            base.Load(instruction, sourceOperand);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            void Write(Cate.ByteRegister register)
            {
                if (pointerRegister.IsIndex()) {
                    instruction.WriteLine("\tld\t" + register + ",(" + pointerRegister + "+" + offset + ")");
                }
                else {
                    instruction.WriteLine("\tld\t" + register + ",(" + pointerRegister + ")");
                }

                instruction.ChangedRegisters.Add(register);
                instruction.RemoveRegisterAssignment(register);
            }

            if (pointerRegister.IsIndex() && pointerRegister.IsOffsetInRange(offset)) {
                Write(this);
                return;
            }
            if (offset == 0) {
                if (Equals(pointerRegister, WordRegister.Hl) || Equals(this, A)) {
                    Write(this);
                    return;
                }
                UsingAccumulator(instruction, () =>
                {
                    Write(A);
                    CopyFrom(instruction, A);
                });
                return;
            }

            if (pointerRegister.Conflicts(this)) {
                WordOperation.UsingAnyRegister(instruction, WordRegister.Registers.Where(r => !Equals(r, pointerRegister)).ToList(), temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, pointerRegister);
                    temporaryRegister.TemporaryOffset(instruction, offset, () =>
                    {
                        LoadIndirect(instruction, temporaryRegister, 0);
                    });
                });
                return;
            }

            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                LoadIndirect(instruction, pointerRegister, 0);
            });
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            void Write(Cate.ByteRegister register)
            {
                if (pointerRegister.IsIndex()) {
                    instruction.WriteLine("\tld\t(" + pointerRegister + "+" + offset + ")," + register);
                }
                else {
                    instruction.WriteLine("\tld\t(" + pointerRegister + ")," + register);
                }
            }

            if (pointerRegister.IsIndex() && pointerRegister.IsOffsetInRange(offset)) {
                Write(this);
                return;
            }
            if (offset == 0) {
                if (Equals(pointerRegister, WordRegister.Hl) || Equals(this, A)) {
                    Write(this);
                    return;
                }
                UsingAccumulator(instruction, () =>
                {
                    A.CopyFrom(instruction, this);
                    Write(A);
                });
                return;
            }
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                StoreIndirect(instruction, pointerRegister, 0);
            });
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            if (Equals(sourceRegister, this)) return;

            instruction.WriteLine("\tld\t" + this + "," + sourceRegister);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + Name);
            }
            instruction.ChangedRegisters.Add(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (!Equals(this, A))
                throw new NotImplementedException();
            switch (operand) {
                case IntegerOperand integerOperand:
                    instruction.WriteLine("\t" + operation + integerOperand.IntegerValue);
                    instruction.RemoveRegisterAssignment(A);
                    return;
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        var register = instruction.GetVariableRegister(variableOperand);
                        if (register is ByteRegister byteRegister) {
                            Debug.Assert(offset == 0);
                            instruction.WriteLine("\t" + operation + byteRegister);
                            return;
                        }
                        WordRegister.UsingAny(instruction, WordRegister.AddableRegisters, pointerRegister =>
                        {
                            pointerRegister.LoadFromMemory(instruction, variable.MemoryAddress(offset));
                            instruction.WriteLine("\t" + operation + "(" + pointerRegister + ")");
                        });
                        return;
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        {
                            var register = instruction.GetVariableRegister(pointer, 0);
                            if (register is WordRegister pointerRegister) {
                                OperateAccumulatorIndirect(instruction, operation, pointerRegister, offset);
                                return;
                            }
                        }
                        WordRegister.UsingAny(instruction, WordRegister.Pointers(offset), pointerRegister =>
                        {
                            pointerRegister.LoadFromMemory(instruction, pointer, 0);
                            OperateAccumulatorIndirect(instruction, operation, pointerRegister, offset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            throw new NotImplementedException();
        }

        public override void Restore(Instruction instruction)
        {
            throw new NotImplementedException();
        }

        private static void OperateAccumulatorIndirect(Instruction instruction, string operation, Cate.WordRegister pointerRegister,
            int offset)
        {
            if (!pointerRegister.IsAddable()) {
                WordRegister.UsingAny(instruction, WordRegister.Pointers(offset),
                    temporaryRegister =>
                {
                    temporaryRegister.CopyFrom(instruction, pointerRegister);
                    OperateAccumulatorIndirect(instruction, operation, temporaryRegister, offset);
                });
                return;
            }
            if (pointerRegister.IsIndex() && pointerRegister.IsOffsetInRange(offset)) {
                instruction.WriteLine("\t" + operation + "(" + pointerRegister + "+" + offset + ")");
                return;
            }
            if (offset == 0 && Equals(pointerRegister, WordRegister.Hl)) {
                instruction.WriteLine("\t" + operation + "(" + pointerRegister + ")");
                return;
            }
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                OperateAccumulatorIndirect(instruction, operation, pointerRegister, 0);
            });
        }
    }
}
