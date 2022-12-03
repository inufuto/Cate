using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Mc6800
{
    internal class ByteRegister : Cate.ByteRegister
    {
        public static List<Cate.ByteRegister> Registers = new List<Cate.ByteRegister>();
        public static ByteRegister A = new ByteRegister(1, "a");
        public static ByteRegister B = new ByteRegister(2, "b");

        public static Cate.ByteRegister FromId(int id)
        {
            return Registers.First(r => r.Id == id);
        }

        private ByteRegister(int id, string name) : base(id, name)
        {
            Registers.Add(this);
        }

        public static void Using(Instruction instruction, Cate.ByteRegister register, Action action)
        {
            if (instruction.IsRegisterInUse(register)) {
                instruction.WriteLine("\tpsh" + register);
                action();
                instruction.WriteLine("\tpul" + register);
                return;
            }
            instruction.BeginRegister(register);
            action();
            instruction.EndRegister(register);
        }

        public static void Using(Instruction instruction, ByteRegister register, Operand operand, Action action)
        {
            if (Equals(operand.Register, register)) {
                action();
                return;
            }
            Using(instruction, register, action);
        }

        public static void UsingAny(Instruction instruction, List<Cate.ByteRegister> candidates, Action<Cate.ByteRegister> action)
        {
            void Invoke(Cate.ByteRegister register)
            {
                instruction.BeginRegister(register);
                action(register);
                instruction.EndRegister(register);
            }

            foreach (var register in candidates.Where(r => !instruction.IsRegisterInUse(r))) {
                Invoke(register);
                return;
            }

            var last = candidates.Last();
            instruction.WriteLine("\tpsh" + last);
            instruction.BeginRegister(last);
            action(last);
            instruction.EndRegister(last);
            instruction.WriteLine("\tpul" + last);
        }

        public static void UsingAny(Instruction instruction, Action<Cate.ByteRegister> action)
        {
            UsingAny(instruction, Registers, action);
        }

        public static void UsingAny(Instruction instruction, Operand operand, Action<Cate.ByteRegister> action)
        {
            if (operand is VariableOperand variableOperand) {
                var variable = variableOperand.Variable;
                var offset = variableOperand.Offset;
                var registerId = instruction.GetVariableRegister(variable, offset);
                if (registerId is ByteRegister byteRegister) {
                    instruction.SetVariableRegister(operand, registerId);
                    action(byteRegister);
                    return;
                }
            }
            UsingAny(instruction, action);
        }

        public static void UsingPair(Instruction instruction, Action action)
        {
            Using(instruction, A, () =>
            {
                Using(instruction, B, action);
            });
        }

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tlda" + Name + "\t#" + value);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void LoadConstant(Instruction instruction, int value)
        {
            if (instruction.IsConstantAssigned(this, value)) return;
            if (value == 0) {
                instruction.WriteLine("\tclr" + Name);
                instruction.ChangedRegisters.Add(this);
                instruction.SetRegisterConstant(this,value);
                instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            }
            base.LoadConstant(instruction, value);
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tlda" + Name + "\t" + variable.MemoryAddress(offset));
            instruction.ChangedRegisters.Add(this);
            instruction.SetVariableRegister(variable, offset, this);
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void StoreToMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tsta" + Name + "\t" + variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister,
            int offset)
        {
            Debug.Assert(Equals(pointerRegister, WordRegister.X));
            while (true) {
                if (pointerRegister.IsOffsetInRange(offset)) {
                    instruction.WriteLine("\tlda" + this + "\t" + offset + ",x");
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    instruction.RemoveRegisterAssignment(this);
                    instruction.ChangedRegisters.Add(this);
                    return;
                }
                pointerRegister.Add(instruction, offset);
                offset = 0;
            }
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(Equals(pointerRegister, WordRegister.X));
            while (true) {
                if (pointerRegister.IsOffsetInRange(offset)) {
                    instruction.WriteLine("\tsta" + this + "\t" + offset + "," + pointerRegister);
                    return;
                }
                pointerRegister.Add(instruction, offset);
                offset = 0;
            }
        }


        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tlda" + Name + "\t" + label);
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            //instruction.RemoveVariableRegisterId(Id);
            instruction.WriteLine("\tsta" + Name + "\t" + label);
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            instruction.WriteLine("\tt" + sourceRegister + this);
            instruction.ResultFlags |= Instruction.Flag.Z;
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + Name);
            }

            if (!change)
                return;
            instruction.RemoveRegisterAssignment(this);
            instruction.ChangedRegisters.Add(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            if (operand is VariableOperand variableOperand) {
                var registerId = instruction.GetVariableRegister(variableOperand);
                if (registerId is ByteRegister rightRegister) {
                    instruction.WriteLine("\tsta" + rightRegister + "\t" + ZeroPage.Byte);
                    instruction.WriteLine("\t" + operation + Name + "\t" + ZeroPage.Byte);
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    goto end;
                }
            }
            Cate.Compiler.Instance.ByteOperation.Operate(instruction, operation + Name, change, operand);

            end:
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            instruction.WriteLine("\t" + operation + Name + "\t" + operand);
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpsh" + Name);
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpul" + Name);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpsh" + this);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpul" + this);
        }
    }
}