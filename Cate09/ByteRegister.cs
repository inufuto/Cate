using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Mc6809
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public static List<Cate.ByteRegister> Registers = new List<Cate.ByteRegister>();
        public static readonly ByteRegister A = new ByteRegister(1, "a");
        public static readonly ByteRegister B = new ByteRegister(2, "b");

        public static Cate.ByteRegister FromId(int id)
        {
            return Registers.First(r => r.Id == id);
        }

        public override Cate.WordRegister? PairRegister => WordRegister.D;

        public ByteRegister(int id, string name) : base(id, name)
        {
            Registers.Add(this);
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tld" + Name + "\t#" + value);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            if (instruction.IsConstantAssigned(this, value)) {
                instruction.AddChanged(this);
                return;
            }
            if (value == 0) {
                instruction.WriteLine("\tclr" + Name);
                instruction.AddChanged(this);
                instruction.SetRegisterConstant(this, 0);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            }
            base.LoadConstant(instruction, value);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tld" + Name + "\t" + variable.MemoryAddress(offset));
            instruction.AddChanged(this);
            instruction.SetVariableRegister(variable, offset, this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tst" + Name + "\t" + variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
        }
        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            Debug.Assert(!Equals(pointerRegister, PointerRegister.D));
            Debug.Assert(pointerRegister.WordRegister != null);
            instruction.WriteLine("\tld" + this + "\t" + WordRegister.OffsetOperand(pointerRegister.WordRegister, offset));
            instruction.ResultFlags |= Instruction.Flag.Z;
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            Debug.Assert(!Equals(pointerRegister, PointerRegister.D));
            Debug.Assert(pointerRegister.WordRegister != null);
            instruction.WriteLine("\tst" + this + "\t" + WordRegister.OffsetOperand(pointerRegister.WordRegister, offset));
        }

        public override void LoadIndirect(Instruction instruction, Variable pointer, int offset)
        {
            if (offset == 0) {
                instruction.WriteLine("\tld" + this + "\t[" + pointer.MemoryAddress(0) + "]");
                instruction.RemoveRegisterAssignment(this);
                instruction.AddChanged(this);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            }
            base.LoadIndirect(instruction, pointer, offset);
        }

        public override void StoreIndirect(Instruction instruction, Variable pointer, int offset)
        {
            if (offset == 0 && instruction.GetVariableRegister(pointer, 0) == null) {
                instruction.WriteLine("\tst" + this + "\t[" + pointer.MemoryAddress(0) + "]");
                return;
            }
            base.StoreIndirect(instruction, pointer, offset);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tld" + Name + "\t" + label);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tst" + Name + "\t" + label);
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            instruction.WriteLine("\ttfr\t" + sourceRegister + "," + this);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }
        public override void Exchange(Instruction instruction, Cate.ByteRegister register)
        {
            instruction.WriteLine("\texg\t" + register + "," + this);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(register);
            instruction.AddChanged(register);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + Name);
            }
            if (change) {
                instruction.RemoveRegisterAssignment(this);
                instruction.AddChanged(this);
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            var registerId = operand.Register;
            if (registerId is ByteRegister rightRegister) {
                instruction.WriteLine("\tst" + rightRegister + "\t" + DirectPage.Byte);
                instruction.WriteLine("\t" + operation + Name + "\t" + DirectPage.Byte);
                instruction.ResultFlags |= Instruction.Flag.Z;
                goto end;
            }
            Cate.Compiler.Instance.ByteOperation.Operate(instruction, operation + Name, change, operand);
        end:
            if (!change)
                return;
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            instruction.WriteLine("\t" + operation + Name + "\t" + operand);
            if (change) {
                instruction.RemoveRegisterAssignment(this);
                instruction.AddChanged(this);
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override bool Conflicts(Cate.Register? register)
        {
            if (register is WordRegister wordRegister && wordRegister.Contains(this)) {
                return true;
            }
            if (register is PointerRegister pointerRegister && pointerRegister.Contains(this)) {
                return true;
            }
            return Equals(this, register);
        }

        public override bool Matches(Cate.Register register)
        {
            return Conflicts(register) || register.Equals(WordRegister.D);
        }

        public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            // save together
            throw new NotImplementedException();
        }

        public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            // save together
            throw new NotImplementedException();
        }
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