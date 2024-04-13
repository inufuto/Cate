using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class ByteOperation : Cate.ByteOperation
    {
        private static ByteOperation? instance;
        public override List<Cate.ByteRegister> Registers => ByteRegister.Registers;
        public override List<Cate.ByteRegister> Accumulators => Registers;

        public ByteOperation()
        {
            instance = this;
        }

        protected override void OperateConstant(Instruction instruction, string operation, string value, int count)
        {
            throw new System.NotImplementedException();
        }

        protected override void OperateMemory(Instruction instruction, string operation, bool change, Variable variable, int offset, int count)
        {
            using (var reservation = ReserveAnyRegister(instruction)) {
                var register = reservation.ByteRegister;
                register.LoadFromMemory(instruction, variable, offset);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t" + register.Name);
                }
                register.StoreToMemory(instruction, variable, offset);
            }
            if (change) {
                instruction.RemoveVariableRegister(variable, offset);
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        protected override void OperateIndirect(Instruction instruction, string operation, bool change, Cate.PointerRegister pointerRegister, int offset,
            int count)
        {
            using (var reservation = ReserveAnyRegister(instruction)) {
                var register = reservation.ByteRegister;
                register.LoadIndirect(instruction, pointerRegister, offset);
                for (var i = 0; i < count; ++i) {
                    instruction.WriteLine("\t" + operation + "\t" + register.Name);
                }
                register.StoreIndirect(instruction, pointerRegister, offset);
            }
            instruction.ResultFlags |= Instruction.Flag.Z;
        }

        public override void StoreConstantIndirect(Instruction instruction, Cate.PointerRegister pointerRegister, int offset, int value)
        {
            var candidates = Registers.Where(r => !r.Conflicts(pointerRegister)).ToList();
            using var reservation = ReserveAnyRegister(instruction, candidates);
            var temporaryRegister = reservation.ByteRegister;
            temporaryRegister.LoadConstant(instruction, value);
            temporaryRegister.StoreIndirect(instruction, pointerRegister, offset);
        }

        public override string ToTemporaryByte(Instruction instruction, Cate.ByteRegister rightRegister)
        {
            throw new System.NotImplementedException();
        }

        public static void Operate(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand)
        {
            if (destinationOperand.SameStorage(leftOperand)) {
                if (Tms99.Compiler.Operate(instruction, operation, rightOperand, destinationOperand)) return;
            }

            void ViaRegister(Cate.ByteRegister re)
            {
                re.Load(instruction, leftOperand);
                var right = Tms99.Compiler.OperandToString(instruction, rightOperand, false);
                if (right != null) {
                    instruction.WriteLine("\t" + operation + "\t" + right + "," + re.Name);
                    instruction.AddChanged(re);
                    instruction.RemoveRegisterAssignment(re);
                }
                else {
                    Debug.Assert(instance != null);
                    var candidates = ByteRegister.Registers.Where(r=>!Equals(r, re)).ToList();
                    using var rightReservation = instance.ReserveAnyRegister(instruction, candidates, rightOperand);
                    var rightRegister = rightReservation.ByteRegister;
                    rightRegister.Load(instruction, rightOperand);
                    instruction.WriteLine("\t" + operation + "\t" + rightRegister.Name + "," +
                                          re.Name);
                    instruction.AddChanged(re);
                    instruction.RemoveRegisterAssignment(re);
                }
            }

            if (destinationOperand.Register is ByteRegister byteRegister && !rightOperand.Conflicts(byteRegister)) {
                ViaRegister(byteRegister);
                return;
            }

            var candidates = ByteRegister.Registers.Where(r => !rightOperand.Conflicts(r)).ToList();
            Debug.Assert(instance != null);
            using var destination = instance.ReserveAnyRegister(instruction, candidates, leftOperand);
            var destinationRegister = destination.ByteRegister;
            ViaRegister(destinationRegister);
            destinationRegister.Store(instruction, destinationOperand);
        }

        public static void OperateConstant(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, string value)
        {
            void ViaRegister(Cate.ByteRegister r)
            {
                r.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + r.Name + "," + value);
                instruction.AddChanged(r);
                instruction.RemoveRegisterAssignment(r);
            }

            if (destinationOperand.Register is ByteRegister byteRegister) {
                ViaRegister(byteRegister);
                return;
            }

            Debug.Assert(instance != null);
            using var reservation = instance.ReserveAnyRegister(instruction, leftOperand);
            ViaRegister(reservation.ByteRegister);
            reservation.ByteRegister.Store(instruction, destinationOperand);
        }
        public static void OperateConstant(Instruction instruction, string operation, AssignableOperand destinationOperand, Operand leftOperand, int value)
        {
            OperateConstant(instruction, operation, destinationOperand, leftOperand, value.ToString());
        }

        public static void Operate(Instruction instruction, string operation, Operand leftOperand, Operand rightOperand)
        {
            Debug.Assert(instance != null);

            var left = Tms99.Compiler.OperandToString(instruction, leftOperand, false);
            var right = Tms99.Compiler.OperandToString(instruction, rightOperand, true);
            if (left != null) {
                if (right != null) {
                    instruction.WriteLine("\t" + operation + "\t" + left + "," + right);
                    return;
                }
                using var reservation = instance.ReserveAnyRegister(instruction);
                var rightRegister = reservation.ByteRegister;
                rightRegister.Load(instruction, rightOperand);
                instruction.WriteLine("\t" + operation + "\t" + left + "," + rightRegister.Name);
                rightRegister.Store(instruction, rightOperand);
                return;
            }

            void OperateRegister(Cate.ByteRegister register)
            {
                register.Load(instruction, leftOperand);
                if (right != null) {
                    instruction.WriteLine("\t" + operation + "\t" + register.Name + "," +
                                          right);
                }
                else {
                    Debug.Assert(instance != null);
                    using var reservation = instance.ReserveAnyRegister(instruction);
                    var rightRegister = reservation.ByteRegister;
                    rightRegister.Load(instruction, rightOperand);
                    instruction.WriteLine("\t" + operation + "\t" + register.Name + "," + rightRegister.Name);
                    rightRegister.Store(instruction, rightOperand);
                }

                if (rightOperand is IndirectOperand { Variable: { Register: null } } indirectOperand) {
                    var offset = indirectOperand.Offset;
                    var candidates = PointerOperation.RegistersToOffset(offset);
                    using var reservation = PointerOperation.ReserveAnyRegister(instruction, candidates);
                    var pointerRegister = reservation.WordRegister;
                    pointerRegister.LoadFromMemory(instruction, indirectOperand.Variable, 0);
                    if (offset == 0) {
                        instruction.WriteLine("\t" + operation + "\t" + register.Name + ",*" + pointerRegister.Name);
                    }
                    else {
                        instruction.WriteLine("\t" + operation + "\t" + register.Name + ",@" + offset + "(" + pointerRegister.Name + ")");
                    }
                }
            }
            if (leftOperand.Register is ByteRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            using (var reservation = instance.ReserveAnyRegister(instruction)) {
                OperateRegister(reservation.ByteRegister);
            }
        }

        public static void OperateConstant(Instruction instruction, string operation, Operand leftOperand, int value)
        {
            void OperateRegister(Cate.ByteRegister register)
            {
                register.Load(instruction, leftOperand);
                instruction.WriteLine("\t" + operation + "\t" + register.Name + "," + ByteRegister.ByteConst(value));
            }

            Debug.Assert(instance != null);
            if (leftOperand.Register is ByteRegister leftRegister) {
                OperateRegister(leftRegister);
                return;
            }
            using var reservation = instance.ReserveAnyRegister(instruction);
            OperateRegister(reservation.ByteRegister);
        }
    }
}
