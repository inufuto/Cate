using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.Mc6800
{
    internal class WordRegister : Cate.WordRegister
    {
        public static WordRegister X = new WordRegister(3, "x");
        private WordRegister(int id, string name) : base(id, name)
        { }

        public static List<Cate.WordRegister> Registers => new List<Cate.WordRegister>() { X };

        public static Cate.WordRegister FromId(int registerId)
        {
            Debug.Assert(registerId == X.Id);
            return X;
        }

        public override bool IsIndex() => true;

        public override void Add(Instruction instruction, int offset)
        {
            if (offset == 0)
                return;
            if (offset > 0 && offset <= 16) {
                while (offset > 0) {
                    instruction.WriteLine("\tinx");
                    --offset;
                }
                instruction.RemoveRegisterAssignment(X);
                return;
            }
            if (offset < 0 && offset >= -16) {
                while (offset < 0) {
                    instruction.WriteLine("\tdex");
                    ++offset;
                }
                instruction.RemoveRegisterAssignment(X);
                return;
            }

            if (offset >= 0 && offset < 0x100) {
                void AddByte(Cate.ByteRegister byteRegister)
                {
                    byteRegister.LoadConstant(instruction, offset);
                    instruction.Compiler.CallExternal(instruction, "Cate.AddX" + byteRegister.Name.ToUpper());
                    instruction.RemoveRegisterAssignment(X);
                }
                if (!instruction.IsRegisterInUse(ByteRegister.A)) {
                    ByteRegister.Using(instruction, ByteRegister.A, () =>
                    {
                        AddByte(ByteRegister.A);
                        instruction.RemoveRegisterAssignment(ByteRegister.A);
                    });
                    return;
                }
                if (!instruction.IsRegisterInUse(ByteRegister.B)) {
                    ByteRegister.Using(instruction, ByteRegister.B, () =>
                    {
                        AddByte(ByteRegister.B);
                        instruction.RemoveRegisterAssignment(ByteRegister.B);
                    });
                    return;
                }
                ByteOperation.UsingAnyRegister(instruction, AddByte);
                return;
            }
            ByteRegister.UsingPair(instruction, () =>
            {
                ByteRegister.A.LoadConstant(instruction, "high " + offset);
                ByteRegister.B.LoadConstant(instruction, "low " + offset);
                instruction.Compiler.CallExternal(instruction, "Cate.AddXAB");
                instruction.RemoveRegisterAssignment(X);
                instruction.RemoveRegisterAssignment(ByteRegister.A);
                instruction.RemoveRegisterAssignment(ByteRegister.B);
            });
        }

        public override bool IsOffsetInRange(int offset) => offset >= 0 && offset <= 0xff;

        public override bool IsPointer(int offset) => true;   //IsOffsetInRange(offset);

        public override void LoadConstant(Instruction instruction, string value)
        {
            instruction.WriteLine("\tldx\t#" + value);
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(X);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            Debug.Assert(Equals(X));
            instruction.WriteLine("\tldx\t" + label);
        }


        public override void Load(Instruction instruction, Operand sourceOperand)
        {
            Debug.Assert(Equals(this, X));

            switch (sourceOperand) {
                case IntegerOperand integerOperand:
                    var value = integerOperand.IntegerValue;
                    if (instruction.IsConstantAssigned(this, value)) return;
                    LoadConstant(instruction, value.ToString());
                    instruction.SetRegisterConstant(this, value);
                    return;
                case PointerOperand pointerOperand:
                    instruction.WriteLine("\tldx\t#" + pointerOperand.MemoryAddress());
                    instruction.RemoveRegisterAssignment(X);
                    return;
                case VariableOperand variableOperand: {
                        var register = instruction.GetVariableRegister(variableOperand);
                        if (Equals(register, X))
                            return;
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        LoadFromMemory(instruction, variable, offset);
                        return;
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        Debug.Assert(pointer.Register == null);
                        X.LoadFromMemory(instruction, pointer, 0);
                        X.LoadIndirect(instruction, WordRegister.X, offset);
                        return;
                    }
            }
            throw new NotImplementedException();
        }


        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            Debug.Assert(Equals(this, X));
            if (Equals(instruction.GetVariableRegister(variable, offset), this))
                return;
            instruction.WriteLine("\tldx\t" + variable.MemoryAddress(offset));
            instruction.ChangedRegisters.Add(this);
            instruction.SetVariableRegister(variable, offset, this);
        }

        public override void Store(Instruction instruction, AssignableOperand destinationOperand)
        {
            Debug.Assert(Equals(this, X));

            switch (destinationOperand) {
                case VariableOperand variableOperand:
                    instruction.WriteLine("\tstx\t" + variableOperand.MemoryAddress());
                    instruction.SetVariableRegister(variableOperand, this);
                    return;
                case IndirectOperand indirectOperand:
                    var pointer = indirectOperand.Variable;
                    var offset = indirectOperand.Offset;
                    instruction.WriteLine("\tstx\t" + ZeroPage.Word);
                    Debug.Assert(pointer.Register == null);
                    X.LoadFromMemory(instruction, pointer, 0);
                    Cate.Compiler.Instance.ByteOperation.UsingAnyRegister(instruction, register =>
                    {
                        instruction.WriteLine("\tlda" + register + "\t" + ZeroPage.WordLow);
                        register.StoreIndirect(instruction, X, offset + 1);
                        instruction.WriteLine("\tlda" + register + "\t" + ZeroPage.WordHigh);
                        register.StoreIndirect(instruction, X, offset);
                    });
                    return;
            }
            throw new NotImplementedException();
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            Debug.Assert(Equals(X));
            instruction.RemoveRegisterAssignment(this);
            instruction.WriteLine("\tstx\t" + label);
        }


        public override void LoadIndirect(Instruction instruction, Cate.WordRegister pointerRegister, int offset)
        {
            Debug.Assert(Equals(this, WordRegister.X));
            Debug.Assert(Equals(pointerRegister, WordRegister.X));
            while (true) {
                if (X.IsOffsetInRange(offset)) {
                    instruction.WriteLine("\tldx\t" + offset + ",x");
                    instruction.ResultFlags |= Instruction.Flag.Z;
                    instruction.RemoveRegisterAssignment(X);
                    return;
                }

                pointerRegister.Add(instruction, offset);
                offset = 0;
            }
        }
        public override void StoreIndirect(Instruction instruction, Cate.WordRegister destinationPointerRegister, int destinationOffset)
        {
            // TODO
            throw new NotImplementedException();
        }

        public override void CopyFrom(Instruction instruction, Cate.WordRegister sourceRegister)
        {
            // Cannot copy because word register is only one
            throw new NotImplementedException();
        }


        //public static void OperateIndirect(Instruction instruction, string operation, int offset, int count)
        //{
        //    while (true) {
        //        if (X.IsOffsetInRange(offset)) {
        //            for (var i = 0; i < count; ++i) {
        //                instruction.WriteLine("\t" + operation + "\t" + offset + ",x");
        //            }
        //            instruction.ResultFlags |= Instruction.Flag.Z;
        //            return;
        //        }
        //        X.Add(instruction, offset);
        //        offset = 0;
        //    }
        //}
        private void OperateConstant(Instruction instruction, string operation, bool change, string value)
        {
            instruction.WriteLine("\t" + operation + Name + "\t#" + value);
            instruction.ResultFlags |= Instruction.Flag.Z;
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        private void OperateConstant(Instruction instruction, string operation, bool change, int value)
        {
            OperateConstant(instruction, operation, change, value.ToString());
        }

        private void OperateMemory(Instruction instruction, string operation, bool change, string label)
        {
            instruction.WriteLine("\t" + operation + Name + "\t" + label);
            if (!change)
                return;
            instruction.ChangedRegisters.Add(this);
            instruction.RemoveRegisterAssignment(this);
        }

        private void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset)
        {
            OperateMemory(instruction, operation, change, variable.MemoryAddress(offset));
        }


        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    Debug.Assert(!change);
                    OperateConstant(instruction, operation, change, integerOperand.IntegerValue);
                    return;
                case PointerOperand pointerOperand:
                    Debug.Assert(!change);
                    OperateConstant(instruction, operation, change, pointerOperand.MemoryAddress());
                    return;
                case StringOperand stringOperand:
                    Debug.Assert(!change);
                    OperateConstant(instruction, operation, change, stringOperand.StringValue);
                    return;
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        var registerId = variable.Register;
                        if (registerId is WordRegister rightRegister) {
                            Debug.Assert(operation.Replace("\t", "").Length == 3);
                            rightRegister.StoreToMemory(instruction, ZeroPage.Word.Name);
                            OperateMemory(instruction, operation, change, ZeroPage.Word.Name);
                            return;
                        }
                        OperateMemory(instruction, operation, change, variable, offset);
                        return;
                    }
                    //case IndirectOperand indirectOperand: {
                    //        var pointer = indirectOperand.Variable;
                    //        var offset = indirectOperand.Offset;
                    //        if (pointer.Register is WordRegister pointerRegister) {
                    //            OperateIndirect(instruction, operation, pointerRegister, offset);
                    //            return;
                    //        }
                    //        OperateIndirect(instruction, operation, pointer, offset);
                    //        return;
                    //    }
            }
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            StoreToMemory(instruction, ZeroPage.Word.Name);
            Cate.Compiler.Instance.ByteOperation.UsingAnyRegister(instruction, register =>
            {
                register.LoadFromMemory(instruction, ZeroPage.Word.High.Name);
                instruction.WriteLine("\tpsh" + register);
                register.LoadFromMemory(instruction, ZeroPage.Word.Low.Name);
                instruction.WriteLine("\tpsh" + register);
            });
        }

        public override void Restore(Instruction instruction)
        {
            Cate.Compiler.Instance.ByteOperation.UsingAnyRegister(instruction, register =>
            {
                instruction.WriteLine("\tpul" + register);
                register.LoadFromMemory(instruction, ZeroPage.Word.Low.Name);
                instruction.WriteLine("\tpul" + register);
                register.LoadFromMemory(instruction, ZeroPage.Word.High.Name);
            });
            LoadFromMemory(instruction, ZeroPage.Word.Name);
        }


        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            // cannot save : don't assign to variable
            throw new NotImplementedException();
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            // cannot save : don't assign to variable
            throw new NotImplementedException();
        }
    }
}