using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class ByteOperation : RegisterOperation<ByteRegister>
    {
        public abstract List<ByteRegister> Accumulators { get; }

        protected virtual void OperateConstant(Instruction instruction, string operation, string value, int count)
        {
            for (var i = 0; i < count; ++i) {
                instruction.WriteLine("\t" + operation + value);
            }
        }

        protected abstract void OperateMemory(Instruction instruction, string operation, bool change, Variable variable,
            int offset, int count);

        protected abstract void OperateIndirect(Instruction instruction, string operation, bool change,
            PointerRegister pointerRegister, int offset, int count);

        protected virtual void OperateIndirect(Instruction instruction, string operation, bool change, Variable pointer,
            int offset, int count)
        {
            using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerOperation.RegistersToOffset(offset));
            reservation.PointerRegister.LoadFromMemory(instruction, pointer, 0);
            OperateIndirect(instruction, operation, change, reservation.PointerRegister, offset, count);
        }


        public virtual void Operate(Instruction instruction, string operation, bool change, Operand operand,
            int count)
        {
            switch (operand) {
                case IntegerOperand integerOperand:
                    OperateConstant(instruction, operation, integerOperand.IntegerValue.ToString(), count);
                    return;
                case StringOperand stringOperand:
                    OperateConstant(instruction, operation, stringOperand.StringValue, count);
                    return;
                case VariableOperand variableOperand: {
                        var variable = variableOperand.Variable;
                        var offset = variableOperand.Offset;
                        var register = variable.Register;
                        if (register is ByteRegister byteRegister) {
                            Debug.Assert(operation.Replace("\t", "").Replace(" ", "").Length == 3);
                            byteRegister.Operate(instruction, operation, change, count);
                            instruction.ResultFlags |= Instruction.Flag.Z;
                            return;
                        }
                        OperateMemory(instruction, operation, change, variable, offset, count);
                        return;
                    }
                case IndirectOperand indirectOperand: {
                        var pointer = indirectOperand.Variable;
                        var offset = indirectOperand.Offset;
                        var variableRegister = instruction.GetVariableRegister(indirectOperand.Variable, 0);
                        if (variableRegister is PointerRegister pointerRegister) {
                            //var pointerRegister = Compiler.Instance.WordOperation.RegisterFromId(pointer.Register.Value);
                            OperateIndirect(instruction, operation, change, pointerRegister, offset, count);
                            return;
                        }
                        OperateIndirect(instruction, operation, change, pointer, offset, count);
                        return;
                    }
                case ByteRegisterOperand byteRegisterOperand: {
                        byteRegisterOperand.Register.Operate(instruction, operation, change, count);
                        instruction.ResultFlags |= Instruction.Flag.Z;
                        return;
                    }
            }
            throw new NotImplementedException();
        }

        public void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            Operate(instruction, operation, change, operand, 1);
        }

        public abstract void StoreConstantIndirect(Instruction instruction, PointerRegister pointerRegister, int offset, int value);

        //public abstract List<ByteRegister> Registers { get; }


        public RegisterReservation ReserveRegister(Instruction instruction, ByteRegister register)
        {
            return instruction.ReserveRegister(register);
        }

        public RegisterReservation ReserveRegister(Instruction instruction, ByteRegister register, Operand operand)
        {
            instruction.CancelOperandRegister(operand, register);
            return instruction.ReserveRegister(register);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<ByteRegister> candidates, Operand sourceOperand)
        {
            if (!(sourceOperand is VariableOperand variableOperand)) return ReserveAnyRegister(instruction, candidates);
            var register = instruction.GetVariableRegister(variableOperand);
            if (!(register is ByteRegister byteRegister) || !candidates.Contains(byteRegister))
                return ReserveAnyRegister(instruction, candidates);
            //instruction.CancelOperandRegister(sourceOperand);
            return ReserveRegister(instruction, byteRegister, sourceOperand);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<ByteRegister> candidates)
        {
            if (Compiler.Instance.IsAssignedRegisterPrior()) {
                foreach (var register in candidates.Where(r => !instruction.IsRegisterReserved(r) && !instruction.IsRegisterInVariableRange(r, null))) {
                    return instruction.ReserveRegister(register);
                }
            }
            foreach (var register in candidates.Where(register => !instruction.IsRegisterReserved(register))) {
                return instruction.ReserveRegister(register);
            }
            return instruction.ReserveRegister(candidates.Last());
        }

        protected virtual void SaveAndRestore(Instruction instruction, ByteRegister register, Action action)
        {
            var temporaryRegister = Registers.Find(r => !Equals(r, register) && !instruction.IsRegisterReserved(r));
            if (temporaryRegister != null) {
                temporaryRegister.CopyFrom(instruction, register);
                action();
                register.CopyFrom(instruction, temporaryRegister);
            }
            else {
                register.Save(instruction);
                action();
                register.Restore(instruction);
            }
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction)
        {
            return ReserveAnyRegister(instruction, Registers);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, Operand sourceOperand)
        {
            return ReserveAnyRegister(instruction, Registers, sourceOperand);
        }
        //public RegisterReservation ReserveAnyRegister(Instruction instruction, AssignableOperand destinationOperand,
        //    Operand sourceOperand)
        //{
        //    return ReserveAnyRegister(instruction, null, sourceOperand);
        //}


        public abstract void ClearByte(Instruction instruction, string label);

        public void OperateByteBinomial(BinomialInstruction instruction, string operation, bool change)
        {
            void ViaRegister(ByteRegister r)
            {
                if (instruction.RightOperand.Register is ByteRegister rightRegister && Equals(rightRegister, r)) {
                    var temporaryByte = ToTemporaryByte(instruction, rightRegister);
                    instruction.CancelOperandRegister(instruction.RightOperand);
                    r.Load(instruction, instruction.LeftOperand);
                    r.Operate(instruction, operation, change, temporaryByte);
                }
                else {
                    r.Load(instruction, instruction.LeftOperand);
                    r.Operate(instruction, operation, change, instruction.RightOperand);
                }

                instruction.RemoveRegisterAssignment(r);
            }

            if (instruction.DestinationOperand.Register is ByteRegister byteRegister && Accumulators.Contains(byteRegister) && !Equals(instruction.RightOperand.Register, byteRegister)) {
                ViaRegister(byteRegister);
                return;
            }

            var candidates = Accumulators.Where(r => !r.Conflicts(instruction.RightOperand.Register)).ToList();
            if (candidates.Count == 0) {
                candidates = Accumulators;
            }
            using var reservation = instruction.ByteOperation.ReserveAnyRegister(instruction, candidates, instruction.LeftOperand);
            ViaRegister(reservation.ByteRegister);
            reservation.ByteRegister.Store(instruction, instruction.DestinationOperand);
        }

        public abstract string ToTemporaryByte(Instruction instruction, ByteRegister register);


        //public RegisterReservation.Saving Save(ByteRegister register, Instruction instruction)
        //{
        //    return new Saving(register, instruction, this);
        //}
    }
}
