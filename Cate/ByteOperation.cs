using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate
{
    public abstract class ByteOperation
    {
        private class Saving : RegisterReservation.Saving
        {
            private readonly ByteRegister register;
            private RegisterReservation? reservation;

            public Saving(ByteRegister register, Instruction instruction, ByteOperation wordOperation)
            {
                this.register = register;
                var candidates = wordOperation.Registers
                    .Where(r => !Equals(r, register) && !instruction.IsRegisterReserved(r)).ToList();
                if (candidates.Any()) {
                    reservation = wordOperation.ReserveAnyRegister(instruction, candidates);
                    reservation.ByteRegister.CopyFrom(instruction, register);
                }
                else {
                    register.Save(instruction);
                }
            }

            public override void Restore(Instruction instruction)
            {
                if (reservation != null) {
                    register.CopyFrom(instruction, reservation.ByteRegister);
                    reservation.Dispose();
                    reservation = null;
                }
                else {
                    register.Restore(instruction);
                }
            }
        }
        protected static WordOperation WordOperation => Compiler.Instance.WordOperation;


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
            WordRegister pointerRegister, int offset, int count);

        protected virtual void OperateIndirect(Instruction instruction, string operation, bool change, Variable pointer,
            int offset, int count)
        {
            var reservation = Compiler.Instance.WordOperation.ReserveAnyRegister(instruction, Compiler.Instance.WordOperation.PointerRegisters(offset));
            reservation.WordRegister.LoadFromMemory(instruction, pointer, 0);
            OperateIndirect(instruction, operation, change, reservation.WordRegister, offset, count);
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
                            //var register = RegisterFromId(Register);
                            byteRegister.Operate(instruction, operation, change, count);
                            instruction.RemoveChanged(byteRegister);
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

        public abstract void StoreConstantIndirect(Instruction instruction, WordRegister pointerRegister, int offset,
            int value);

        public abstract List<ByteRegister> Registers { get; }


        public ByteRegister TemporaryRegister(Instruction instruction, IEnumerable<ByteRegister> candidates)
        {
            var register = candidates.First(r => !instruction.IsRegisterReserved(r));
            Debug.Assert(register != null);
            return register;
        }

        public RegisterReservation ReserveRegister(Instruction instruction, ByteRegister register)
        {
            //if (instruction.IsRegisterReserved(register)) {
            //    var candidates = Registers.Where(r => !Equals(r, register) && !instruction.IsRegisterReserved(r)).ToList();
            //    if (candidates.Any()) {
            //        var rr = ReserveAnyRegister(instruction, candidates);
            //            , otherRegister =>
            //        {
            //            otherRegister.CopyFrom(instruction, register);
            //            action();
            //            register.CopyFrom(instruction, otherRegister);
            //            instruction.AddChanged(otherRegister);
            //            instruction.RemoveRegisterAssignment(otherRegister);
            //        });
            //        return;
            //    }
            //    register.Save(instruction);
            //    action();
            //    register.Restore(instruction);
            //    return;
            //}
            //instruction.ReserveRegister(register);
            //action();
            //instruction.CancelRegister(register);
            return instruction.ReserveRegister(register);
        }

        public RegisterReservation ReserveRegister(Instruction instruction, ByteRegister register, Operand operand)
        {
            //if (Equals(operand.Register, register)) {
            instruction.CancelOperandRegister(operand, register);
            //}
            return instruction.ReserveRegister(register);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<ByteRegister> candidates,
            AssignableOperand? destinationOperand, Operand? sourceOperand)
        {
            {
                if (destinationOperand?.Register is ByteRegister byteRegister && candidates.Contains(byteRegister)) {
                    if (Equals(sourceOperand?.Register, byteRegister)) {
                        instruction.CancelOperandRegister(sourceOperand);
                        instruction.CancelOperandRegister(sourceOperand);
                    }
                    return instruction.ReserveRegister(byteRegister);
                }
            }
            if (!(sourceOperand is VariableOperand variableOperand)) return ReserveAnyRegister(instruction, candidates);
            {
                var register = instruction.GetVariableRegister(variableOperand);
                if (!(register is ByteRegister byteRegister) || !candidates.Contains(byteRegister))
                    return ReserveAnyRegister(instruction, candidates);
                //instruction.CancelOperandRegister(sourceOperand);
                return ReserveRegister(instruction, byteRegister, sourceOperand);
            }
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, AssignableOperand? destinationOperand,
            Operand? sourceOperand)
        {
            return ReserveAnyRegister(instruction, Registers, destinationOperand, sourceOperand);
        }

        public RegisterReservation ReserveAnyRegister(Instruction instruction, List<ByteRegister> candidates)
        {
            //void Invoke(Cate.ByteRegister register)
            //{
            //    instruction.ReserveRegister(register);
            //    action(register);
            //    instruction.CancelRegister(register);
            //}

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

        public RegisterReservation ReserveAnyRegisterToChange(Instruction instruction, List<ByteRegister> candidates,
            AssignableOperand destinationOperand, Operand sourceOperand)
        {
            instruction.CancelOperandRegister(sourceOperand);
            if (destinationOperand.Register is ByteRegister destinationRegister) {
                //instruction.CancelOperandRegister(sourceOperand);
                if (candidates.Contains(destinationRegister)) {
                    return instruction.ReserveRegister(destinationRegister);
                }
            }

            if (!(sourceOperand.Register is ByteRegister sourceRegister) ||
                !instruction.IsRegisterReserved(sourceOperand.Register) ||
                !candidates.Contains(sourceRegister)) return ReserveAnyRegister(instruction, candidates);
            return instruction.ReserveRegister(sourceRegister);
        }

        public RegisterReservation ReserveAnyRegisterToChange(Instruction instruction, AssignableOperand destinationOperand,
            Operand sourceOperand)
        {
            return ReserveAnyRegisterToChange(instruction, Registers, destinationOperand, sourceOperand);
        }


        public abstract void ClearByte(Instruction instruction, string label);

        //public void UsingAnyRegister(Instruction instruction, List<ByteRegister> candidates, Operand operand,
        //    Action<ByteRegister> action)
        //{
        //    if (operand is VariableOperand variableOperand) {
        //        if (variableOperand.Register is ByteRegister register) {
        //            if (candidates.Contains(register)) {
        //                action(register);
        //                return;
        //            }
        //        }
        //    }
        //    UsingAnyRegister(instruction, candidates, action);
        //}

        public void OperateByteBinomial(BinomialInstruction instruction, string operation, bool change)
        {
            var candidates = Accumulators.Where(r => !r.Conflicts(instruction.RightOperand.Register)).ToList();
            if (candidates.Count == 0) {
                candidates = Accumulators;
            }
            using var reservation = instruction.ByteOperation.ReserveAnyRegister(instruction, candidates,
                instruction.DestinationOperand, instruction.LeftOperand);
            if (instruction.RightOperand.Register is ByteRegister rightRegister && Equals(rightRegister, reservation.ByteRegister)) {
                var temporaryByte = ToTemporaryByte(instruction, rightRegister);
                instruction.CancelOperandRegister(instruction.RightOperand);
                reservation.ByteRegister.Load(instruction, instruction.LeftOperand);
                reservation.ByteRegister.Operate(instruction, operation, change, temporaryByte);
            }
            else {
                reservation.ByteRegister.Load(instruction, instruction.LeftOperand);
                reservation.ByteRegister.Operate(instruction, operation, change, instruction.RightOperand);
            }
            instruction.RemoveRegisterAssignment(reservation.ByteRegister);
            reservation.ByteRegister.Store(instruction, instruction.DestinationOperand);
        }

        public abstract string ToTemporaryByte(Instruction instruction, ByteRegister register);


        public RegisterReservation.Saving Save(ByteRegister register, Instruction instruction)
        {
            return new Saving(register, instruction, this);
        }

        public List<ByteRegister> RegistersOtherThan(ByteRegister register)
        {
            return Registers.Where(r => !Equals(r, register)).ToList();
        }
    }
}
