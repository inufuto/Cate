using System;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.MuCom87
{
    internal class Accumulator : ByteRegister
    {
        protected internal Accumulator(int id) : base(id, "a") { }


        public override void LoadIndirect(Instruction instruction, Cate.PointerRegister pointerRegister)
        {
            instruction.WriteLine("\tldax\t" + pointerRegister.AsmName);
            instruction.AddChanged(A);
            instruction.RemoveRegisterAssignment(A);
        }

        public override void StoreIndirect(Instruction instruction, Cate.PointerRegister pointerRegister)
        {
            instruction.WriteLine("\tstax\t" + pointerRegister.AsmName);
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            instruction.WriteLine("\tmov\ta," + sourceRegister.AsmName);
            instruction.AddChanged(this);
            instruction.RemoveRegisterAssignment(this);
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    instruction.WriteLine("\t" + operation.Split('|')[1] + "\ta," + integerOperand.IntegerValue);
                    instruction.RemoveRegisterAssignment(A);
                    return;
                case StringOperand stringOperand:
                    instruction.WriteLine("\t" + operation.Split('|')[1] + "\ta," + stringOperand.StringValue);
                    instruction.RemoveRegisterAssignment(A);
                    return;
                case ByteRegisterOperand registerOperand: {
                        if (registerOperand.Register is ByteRegister byteRegister) instruction.WriteLine("\t" + operation.Split('|')[0] + "\ta," + byteRegister.AsmName);
                        return;
                    }
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        var register = instruction.GetVariableRegister(variableOperand);
                        switch (register) {
                            case ByteRegister byteRegister:
                                Debug.Assert(offset == 0);
                                instruction.WriteLine("\t" + operation.Split('|')[0] + "\ta," + byteRegister);
                                return;
                            default:
                                using (var reservation = WordOperation.ReserveAnyRegister(instruction, WordRegister.Registers)) {
                                    var pointerRegister = reservation.WordRegister;
                                    pointerRegister.LoadConstant(instruction, variable.MemoryAddress(offset));
                                    Debug.Assert(pointerRegister.High != null);
                                    instruction.WriteLine("\t" + operation.Split('|')[0] + "x\t" + pointerRegister.AsmName);
                                }
                                return;
                        }
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        {
                            var register = instruction.GetVariableRegister(pointer, 0);
                            if (register is PointerRegister pointerRegister) {
                                OperateIndirect(instruction, operation, pointerRegister, offset);
                                return;
                            }
                        }
                        {
                            using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.Registers);

                            var pointerRegister = reservation.PointerRegister;
                            pointerRegister.LoadFromMemory(instruction, pointer, 0);
                            OperateIndirect(instruction, operation, pointerRegister, offset);
                        }
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            instruction.WriteLine("\t" + operation + "a," + operand);
        }

        private static void OperateIndirect(Instruction instruction, string operation, Cate.PointerRegister pointerRegister, int offset)
        {
            if (pointerRegister.IsOffsetInRange(offset)) {
                OperateIndirect(instruction, operation, pointerRegister);
                return;
            }

            pointerRegister.TemporaryOffset(instruction, offset,
                () => { OperateIndirect(instruction, operation, pointerRegister); });
        }

        private static void OperateIndirect(Instruction instruction, string operation, Cate.PointerRegister pointerRegister)
        {
            instruction.WriteLine("\t" + operation.Split('|')[0] + "x\t" + pointerRegister.AsmName);
        }

        public override void Save(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpush\tv" + comment);
        }

        public override void Restore(StreamWriter writer, string? comment, bool jump, int tabCount)
        {
            Instruction.WriteTabs(writer, tabCount);
            writer.WriteLine("\tpop\tv" + comment);
        }

        public override void Save(Instruction instruction)
        {
            instruction.WriteLine("\tpush\tv");
        }

        public override void Restore(Instruction instruction)
        {
            instruction.WriteLine("\tpop\tv");
        }
    }
}
