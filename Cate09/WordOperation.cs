using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate.Mc6809
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;

        public static void Operate(Instruction instruction, string operation, bool change, Operand operand, int count)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    OperateConstant(instruction, operation, integerOperand.IntegerValue.ToString(), count);
                    return;
                case PointerOperand pointerOperand:
                    OperateConstant(instruction, operation, pointerOperand.MemoryAddress(), count);
                    return;
                case StringOperand stringOperand:
                    OperateConstant(instruction, operation, stringOperand.StringValue, count);
                    return;
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        var registerId = variable.Register;
                        if (registerId is WordRegister register) {
                            Debug.Assert(operation.Replace("\t", "").Length == 3);
                            register.Operate(instruction, operation, count);
                            instruction.ResultFlags |= Instruction.Flag.Z;
                            return;
                        }
                        OperateMemory(instruction, operation, change, variable, offset, count);
                        return;
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        if (pointer.Register is WordRegister pointerRegister) {
                            OperateIndirect(instruction, operation, pointerRegister, offset, count);
                            return;
                        }
                        OperateIndirect(instruction, operation, pointer, offset, count);
                        return;
                    }
            }
            throw new NotImplementedException();
        }


        private static void OperateConstant(Instruction instruction, string operation, string value, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t#" + value);
            }
        }

        private static void OperateMemory(Instruction instruction, string operation, bool change, Variable variable,
            int offset, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + "\t" + variable.MemoryAddress(offset));
            }
            if (change) {
                instruction.RemoveVariableRegister(variable, offset);
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        private static void OperateIndirect(Instruction instruction, string operation, Cate.WordRegister pointerRegister, int offset, int count)
        {
            if (offset == 0) {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t," + pointerRegister);
                }
            }
            else {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t" + offset + "," + pointerRegister);
                }
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        private static void OperateIndirect(Instruction instruction, string operation, Variable pointer, int offset, int count)
        {
            if (offset == 0) {
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t[" + pointer.MemoryAddress(0) + "]");
                }
                return;
            }
            Cate.Compiler.Instance.WordOperation.UsingAnyRegister(instruction, WordRegister.IndexRegisters, pointerRegister =>
            {
                pointerRegister.LoadFromMemory(instruction, pointer, 0);
                OperateIndirect(instruction, operation, pointerRegister, offset, count);
            });
        }
    }
}
