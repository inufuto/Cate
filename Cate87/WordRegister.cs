using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.MuCom87
{
    internal class WordRegister : Cate.WordRegister
    {
        public static readonly WordRegister Hl = new(11, ByteRegister.H, ByteRegister.L);
        public static readonly WordRegister De = new(12, ByteRegister.D, ByteRegister.E);
        public static readonly WordRegister Bc = new(13, ByteRegister.B, ByteRegister.C);

        public static List<Cate.WordRegister> Registers = new List<Cate.WordRegister>() { Hl, De, Bc };

        public override Cate.ByteRegister? Low { get; }
        public override Cate.ByteRegister? High { get; }

        public override string AsmName => High != null ? High.AsmName : base.AsmName;

        public IEnumerable<Register> ByteRegisters
        {
            get
            {
                Debug.Assert(Low != null);
                Debug.Assert(High != null);
                return new Register[] { Low, High };
            }
        }

        public WordRegister(int id, Cate.ByteRegister high, Cate.ByteRegister low) : base(id, high.Name + low.Name)
        {
            High = high;
            Low = low;
        }


        public override void Save(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpush\t" + AsmName + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpop\t" + AsmName + comment);
        }

        public override bool Conflicts(Register? register)
        {
            if (register is ByteRegister byteRegister && Contains(byteRegister)) {
                return true;
            }
            return base.Conflicts(register);
        }

        //public override void Add(Instruction instruction, int offset)
        //{
        //    if (offset == 0) { return; }

        //    if (offset > 0) {
        //        if (offset < 8) {
        //            var count = offset;
        //            while (count > 0) {
        //                instruction.WriteLine("\tinx\t" + HighName);
        //                --count;
        //            }
        //            instruction.RemoveRegisterAssignment(this);
        //            instruction.AddChanged(this);
        //            return;
        //        }
        //        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
        //            Debug.Assert(Low != null);
        //            Debug.Assert(High != null);
        //            ByteRegister.A.CopyFrom(instruction, Low);
        //            instruction.WriteLine("\tadi\ta,low " + offset);
        //            Low.CopyFrom(instruction, ByteRegister.A);
        //            ByteRegister.A.CopyFrom(instruction, High);
        //            instruction.WriteLine("\taci\ta,high " + offset);
        //            High.CopyFrom(instruction, ByteRegister.A);
        //        }
        //    }
        //    else {
        //        if (offset > -8) {
        //            var count = -offset;
        //            while (count > 0) {
        //                instruction.WriteLine("\tdcx\t" + HighName);
        //                --count;
        //            }
        //            return;
        //        }
        //        using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
        //            Debug.Assert(Low != null);
        //            Debug.Assert(High != null);
        //            ByteRegister.A.CopyFrom(instruction, Low);
        //            instruction.WriteLine("\tsui\ta,low " + -offset);
        //            Low.CopyFrom(instruction, ByteRegister.A);
        //            ByteRegister.A.CopyFrom(instruction, High);
        //            instruction.WriteLine("\tsbi\ta,high " + -offset);
        //            High.CopyFrom(instruction, ByteRegister.A);
        //        }
        //    }
        //}

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
            instruction.WriteLine("\tlxi\t" + AsmName + "," + value);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void LoadFromMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\tl" + Name + "d\t" + label);
            instruction.RemoveRegisterAssignment(this);
            instruction.AddChanged(this);
        }

        public override void StoreToMemory(Instruction instruction, string label)
        {
            instruction.WriteLine("\ts" + Name + "d\t" + label);
        }

        //public override void Load(Instruction instruction, Operand operand)
        //{
        //    switch (operand) {
        //        case IntegerOperand sourceIntegerOperand:
        //            var value = sourceIntegerOperand.IntegerValue;
        //            LoadConstant(instruction, value.ToString());
        //            return;
        //        case PointerOperand sourcePointerOperand:
        //            LoadConstant(instruction, sourcePointerOperand.MemoryAddress());
        //            return;
        //        case VariableOperand sourceVariableOperand: {
        //                var sourceVariable = sourceVariableOperand.Variable;
        //                var sourceOffset = sourceVariableOperand.Offset;
        //                if (sourceVariable.Register is Cate.WordRegister sourceRegister) {
        //                    Debug.Assert(sourceOffset == 0);
        //                    if (!Equals(sourceRegister, this)) {
        //                        CopyFrom(instruction, sourceRegister);
        //                        instruction.AddChanged(this);
        //                        instruction.RemoveRegisterAssignment(this);
        //                    }
        //                    return;
        //                }
        //                LoadFromMemory(instruction, sourceVariable, sourceOffset);
        //                //instruction.CancelOperandRegister(sourceVariableOperand);
        //                return;
        //            }
        //        case IndirectOperand sourceIndirectOperand: {
        //                var pointer = sourceIndirectOperand.Variable;
        //                var offset = sourceIndirectOperand.Offset;
        //                if (pointer.Register is WordRegister pointerRegister) {
        //                    if (!Equals(pointerRegister, this)) {
        //                        LoadIndirect(instruction, pointerRegister, offset);
        //                    }
        //                    else {
        //                        using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers);
        //                        var temporaryRegister = reservation.WordRegister;
        //                        temporaryRegister.CopyFrom(instruction, pointerRegister);
        //                        LoadIndirect(instruction, temporaryRegister, offset);
        //                    }
        //                    //instruction.CancelOperandRegister(sourceIndirectOperand);
        //                    return;
        //                }

        //                using (var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers)) {
        //                    var temporaryRegister = reservation.WordRegister;
        //                    temporaryRegister.LoadFromMemory(instruction, pointer, 0);
        //                    LoadIndirect(instruction, temporaryRegister, offset);
        //                }
        //                //instruction.CancelOperandRegister(sourceIndirectOperand);
        //                return;
        //            }
        //    }
        //    throw new NotImplementedException();
        //}

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
                        if (destinationPointer.Register is Cate.PointerRegister destinationPointerRegister) {
                            StoreIndirect(instruction, destinationPointerRegister, destinationOffset);
                            return;
                        }
                        using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.Registers);
                        var pointerRegister = reservation.PointerRegister;
                        pointerRegister.LoadFromMemory(instruction, destinationPointer, 0);
                        StoreIndirect(instruction, pointerRegister, destinationOffset);
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public override void LoadFromMemory(Instruction instruction, Variable variable, int offset)
        {
            instruction.WriteLine("\tl" + Name + "d\t" + variable.MemoryAddress(offset));
            instruction.SetVariableRegister(variable, offset, this);
            instruction.AddChanged(this);
            instruction.SetRegisterOffset(this, offset);
        }

        private void Store(Instruction instruction, Variable variable, int offset)
        {
            var destinationAddress = variable.MemoryAddress(offset);
            instruction.WriteLine("\ts" + Name + "d\t" + destinationAddress);
            instruction.SetVariableRegister(variable, offset, this);
        }



        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            if (offset == 0) {
                LoadIndirect(instruction, pointerRegister);
                return;
            }
            {
                if (Equals(pointerRegister.WordRegister, this)) {
                    pointerRegister.Add(instruction, offset);
                    LoadIndirect(instruction, pointerRegister);
                    return;
                }

                pointerRegister.TemporaryOffset(instruction, offset, () => { LoadIndirect(instruction, pointerRegister); });
                return;
            }

            throw new NotImplementedException();
        }

        private void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister)
        {
            if (Equals(pointerRegister.WordRegister, this)) {
                using var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.RegistersOtherThan(this));
                var temporaryRegister = reservation.WordRegister;
                var register = ((WordRegister)temporaryRegister);
                register.LoadIndirect(instruction, pointerRegister);
                instruction.AddChanged(register);
                CopyFrom(instruction, temporaryRegister);
                return;
            }
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                Debug.Assert(Low != null);
                Debug.Assert(High != null);
                ByteRegister.A.LoadIndirect(instruction, pointerRegister);
                Low.CopyFrom(instruction, ByteRegister.A);
                instruction.WriteLine("\tinx\t" + pointerRegister.AsmName);
                ByteRegister.A.LoadIndirect(instruction, pointerRegister);
                High.CopyFrom(instruction, ByteRegister.A);
                instruction.WriteLine("\tdcx\t" + pointerRegister.AsmName);
            }
            instruction.AddChanged(this);
        }

        private static List<Cate.WordRegister> RegistersOtherThan(WordRegister register)
        {
            return WordOperation.RegistersOtherThan(register);
            //return Registers.FindAll(r => !Equals(r, register));
        }

        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset)
        {
            if (offset == 0) {
                StoreIndirect(instruction, pointerRegister);
                return;
            }

            {
                pointerRegister.TemporaryOffset(instruction, offset,
                    () => { StoreIndirect(instruction, pointerRegister); });
                return;
            }
        }

        protected virtual void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister)
        {
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                Debug.Assert(Low != null);
                Debug.Assert(High != null);
                ByteRegister.A.CopyFrom(instruction, Low);
                ByteRegister.A.StoreIndirect(instruction, pointerRegister);
                instruction.WriteLine("\tinx\t" + pointerRegister.AsmName);
                ByteRegister.A.CopyFrom(instruction, High);
                ByteRegister.A.StoreIndirect(instruction, pointerRegister);
                instruction.WriteLine("\tdcx\t" + pointerRegister.AsmName);
                instruction.AddChanged(ByteRegister.A);
            }
        }

        public override void CopyFrom(Instruction instruction, Cate.WordRegister register)
        {
            using (ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                Debug.Assert(register.Low != null);
                ByteRegister.A.CopyFrom(instruction, register.Low);
                Debug.Assert(Low != null);
                Low.CopyFrom(instruction, ByteRegister.A);
                Debug.Assert(register.High != null);
                ByteRegister.A.CopyFrom(instruction, register.High);
                Debug.Assert(High != null);
                High.CopyFrom(instruction, ByteRegister.A);
            }
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            throw new NotImplementedException();
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpush\t" + AsmName);
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpop\t" + AsmName);
        }

        protected virtual bool IsOffsetShort(int offset) => Math.Abs(offset) <= 4;
    }
}
