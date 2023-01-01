using System;
using System.Diagnostics;
using System.IO;

namespace Inu.Cate.MuCom87
{
    internal class Accumulator : ByteRegister
    {
        protected internal Accumulator(int id) : base(id, "a") { }


        public override void LoadIndirect(Instruction instruction, Cate.WordRegister wordRegister)
        {
            instruction.WriteLine("\tldax\t" + ((WordRegister)wordRegister).HighName);
            instruction.ChangedRegisters.Add(A);
            instruction.RemoveRegisterAssignment(A);
        }

        public override void StoreIndirect(Instruction instruction, Cate.WordRegister wordRegister)
        {
            instruction.WriteLine("\tstax\t" + ((WordRegister)wordRegister).HighName);
        }

        public override void CopyFrom(Instruction instruction, Cate.ByteRegister sourceRegister)
        {
            switch (sourceRegister) {
                case ByteRegister byteRegister:
                    instruction.WriteLine("\tmov\ta," + byteRegister.Name);
                    instruction.ChangedRegisters.Add(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
                //case ByteWorkingRegister workingRegister:
                //    instruction.WriteLine("\tldaw\t" + workingRegister.Name);
                //    instruction.ChangedRegisters.Add(this);
                //    instruction.RemoveRegisterAssignment(this);
                //    return;
            }
            throw new NotImplementedException();
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
                        switch (registerOperand.Register) {
                            case ByteRegister byteRegister:
                                instruction.WriteLine("\t" + operation.Split('|')[0] + "\ta," + byteRegister.Name);
                                break;
                            //case ByteWorkingRegister workingRegister:
                            //    instruction.WriteLine("\t" + operation.Split('|')[0] + "w\t" + workingRegister.Name);
                            //    break;
                        }
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
                            //case ByteWorkingRegister workingRegister:
                            //    Debug.Assert(offset == 0);
                            //    instruction.WriteLine("\t" + operation.Split('|')[0] + "w\t" + workingRegister.Name);
                            //    return;
                            default:
                                WordOperation.UsingAnyRegister(instruction, WordRegister.Registers, pointerRegister =>
                                {
                                    pointerRegister.LoadConstant(instruction, variable.MemoryAddress(offset));
                                    Debug.Assert(pointerRegister.High != null);
                                    instruction.WriteLine("\t" + operation.Split('|')[0] + "x\t" + pointerRegister.High.Name);
                                });
                                return;
                        }
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        {
                            var register = instruction.GetVariableRegister(pointer, 0);
                            if (register is WordRegister pointerRegister) {
                                OperateIndirect(instruction, operation, pointerRegister, offset);
                                return;
                            }
                        }
                        WordOperation.UsingAnyRegister(instruction, WordRegister.Registers, pointerRegister =>
                        {
                            pointerRegister.LoadFromMemory(instruction, pointer, 0);
                            OperateIndirect(instruction, operation, pointerRegister, offset);
                        });
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public override void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            instruction.WriteLine("\t" + operation + "a," + operand);
        }

        private void OperateIndirect(Instruction instruction, string operation, Cate.WordRegister pointerRegister, int offset)
        {
            switch (pointerRegister) {
                case WordRegister wordRegister when offset == instruction.GetRegisterOffset(wordRegister):
                    OperateIndirect(instruction, operation, wordRegister);
                    return;
                case WordRegister wordRegister:
                    wordRegister.TemporaryOffset(instruction, offset, () =>
                    {
                        OperateIndirect(instruction, operation, wordRegister);
                    });
                    return;
            }
            throw new NotImplementedException();
        }

        private static void OperateIndirect(Instruction instruction, string operation, WordRegister wordRegister)
        {
            instruction.WriteLine("\t" + operation.Split('|')[0] + "x\t" + wordRegister.HighName);
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
