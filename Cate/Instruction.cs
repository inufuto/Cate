﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate
{
    public abstract class Instruction
    {
        [Flags]
        public enum Flag
        {
            Z = 1,
        }

        public abstract class RegisterAssignment
        {
        }

        public class RegisterConstantAssignment : RegisterAssignment
        {
            public readonly Constant Constant;

            public RegisterConstantAssignment(Constant constant)
            {
                Constant = constant;
            }

            public RegisterConstantAssignment(int value) : this(new ConstantInteger(value)) { }

            public override string ToString()
            {
                return Constant.ToString() ?? string.Empty;
            }
        }


        public class RegisterVariableAssignment : RegisterAssignment
        {
            public readonly Variable Variable;
            public readonly int Offset;

            public RegisterVariableAssignment(Variable variable, int offset)
            {
                Variable = variable;
                Offset = offset;
            }

            public override string ToString()
            {
                return Variable + "+" + Offset;
            }

            public override bool Equals(object? obj)
            {
                if (obj is RegisterVariableAssignment o) {
                    return o.Variable.Equals(Variable) && o.Offset == Offset;
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Variable.GetHashCode() + Offset.GetHashCode();
            }
        }

        public readonly int Address;
        public readonly ISet<Instruction> PreviousInstructions = new HashSet<Instruction>();
        public readonly Function Function;
        public readonly IList<string> Codes = new List<string>();
        private readonly List<string> codesToJump = new List<string>();
        private readonly IList<RegisterReservation> registerReservations = new List<RegisterReservation>();
        private readonly IList<RegisterReservation> operandRegisterReservations = new List<RegisterReservation>();
        //private readonly Dictionary<Register, int> sourceRegisters = new Dictionary<Register, int>();
        //private readonly ISet<Register> sourceRegisters2 = new HashSet<Register>();
        //private readonly ISet<Register> temporaryRegisters = new HashSet<Register>();
        private readonly ISet<Register> changedRegisters = new HashSet<Register>();
        public readonly ISet<Variable> SavingVariables = new HashSet<Variable>();
        public readonly IDictionary<Register, RegisterAssignment> RegisterAssignments = new Dictionary<Register, RegisterAssignment>();
        public Flag ResultFlags;
        public readonly Dictionary<WordRegister, int> RegisterOffsets = new Dictionary<WordRegister, int>();

        protected Instruction(Function function)
        {
            Function = function;
            Address = function.NextAddress;

            if (Address <= 0)
                return;
            var previousInstruction = function.Instructions[Address - 1];
            if (!previousInstruction.IsUnconditionalJump()) {
                PreviousInstructions.Add(previousInstruction);
            }
        }

        public Compiler Compiler => Compiler.Instance;
        public ByteOperation ByteOperation => Compiler.Instance.ByteOperation;
        public WordOperation WordOperation => Compiler.Instance.WordOperation;
        public PointerOperation PointerOperation => Compiler.Instance.PointerOperation;

        public virtual bool IsJump() => false;

        public virtual bool IsUnconditionalJump() => false;

        public bool IsEmpty()
        {
            return !Codes.Any() && !codesToJump.Any();
        }


        public ISet<Register> TemporaryRegisters
        {
            get
            {
                var temporaryRegisterIds = new HashSet<Register>();
                var registers = ResultRegister != null ? changedRegisters.Where(r => !Equals(r, ResultRegister)) : changedRegisters;
                foreach (var changedRegister in registers) {
                    Compiler.Instance.AddSavingRegister(temporaryRegisterIds, changedRegister);
                }
                return temporaryRegisterIds;
            }
        }

        protected virtual Register? ResultRegister => ResultOperand?.Register;

        public virtual Operand? ResultOperand => null;

        public Register? GetVariableRegister(Variable variable, int offset, Func<Register, bool>? func)
        {
            bool IsMatch(Register r) => func == null || func(r);

            var registers = new List<Register>();

            if (offset == 0 && variable.Register != null) {
                registers.Add(variable.Register);
            }

            foreach (var (register, assignment) in RegisterAssignments) {
                if (
                    assignment is RegisterVariableAssignment variableAssignment &&
                    variableAssignment.Variable.SameStorage(variable) &&
                    register.ByteCount == variable.FirstType!.ByteCount &&
                    variableAssignment.Offset == offset &&
                    IsMatch(register)
                )
                    registers.Add(register);
            }
            if (registers.Count == 0) return null;
            if (func != null) {
                var register = registers.FirstOrDefault(func);
                //if (register != null) 
                return register;
            }
            return registers[0];
        }

        public Register? GetVariableRegister(Variable variable, int offset)
        {
            return GetVariableRegister(variable, offset, null);
        }

        public Register? GetVariableRegister(VariableOperand operand, Func<Register, bool>? func)
        {
            return GetVariableRegister(operand.Variable, operand.Offset, func);
        }

        public Register? GetVariableRegister(VariableOperand operand)
        {
            return GetVariableRegister(operand, null);
        }

        public bool VariableRegisterMatches(Variable variable, int offset, Register register)
        {
            if (Equals(variable.Register, register) && offset == 0)
                return true;
            if (
                RegisterAssignments.TryGetValue(register, out var assignment) &&
                assignment is RegisterVariableAssignment variableAssignment
            ) {
                return variableAssignment.Variable.SameStorage(variable) && variableAssignment.Offset == offset;
            }
            return false;
        }

        public bool VariableRegisterMatches(VariableOperand operand, Register register)
        {
            return VariableRegisterMatches(operand.Variable, operand.Offset, register);
        }

        public void SetVariableRegister(Operand operand, Register? register)
        {
            if (operand is not VariableOperand variableOperand) {
                if (register != null) {
                    RemoveRegisterAssignment(register);
                }
                return;
            }
            RemoveVariableRegister(variableOperand);
            var variable = variableOperand.Variable;
            var offset = variableOperand.Offset;
            SetVariableRegister(variable, offset, register);
        }

        public void SetVariableRegister(Variable variable, int offset, Register? register)
        {
            if (IsRegisterInVariableRange(register, variable)) {
                if (register != null) {
                    RemoveRegisterAssignment(register);
                    RemoveVariableRegister(variable, offset);
                }
                return;
            }

            var resultRegisterUsage = new RegisterVariableAssignment(variable, offset);

            if (register != null) {
                RegisterAssignments[register] = resultRegisterUsage;
                if (register is WordRegister wordRegister) {
                    if (wordRegister.Low != null) {
                        RegisterAssignments.Remove(wordRegister.Low);
                        //RemoveRegisterAssignment(wordRegister.Low);
                    }
                    if (wordRegister.High != null) {
                        RegisterAssignments.Remove(wordRegister.High);
                        //RemoveRegisterAssignment(wordRegister.High);
                    }
                }
            }
        }

        public void RemoveRegisterAssignment(Register register)
        {
            RegisterAssignments.Remove(register);
            foreach (var pair in RegisterAssignments) {
                var key = pair.Key;
                if (register.Conflicts(key)) {
                    RegisterAssignments.Remove(key);
                }
            }
        }

        public void RemoveVariableRegister(Operand operand)
        {
            if (!(operand is VariableOperand variableOperand))
                return;
            var variable = variableOperand.Variable;
            var offset = variableOperand.Offset;
            RemoveVariableRegister(variable, offset);
        }

        public void RemoveVariableRegister(Variable variable, int offset)
        {
            foreach (var (key, assignment) in RegisterAssignments) {
                if (!(assignment is RegisterVariableAssignment variableAssignment)) continue;
                var remove = false;
                if (variableAssignment.Variable.SameStorage(variable)) {
                    if (variableAssignment.Offset == offset) {
                        remove = true;
                    }
                    else if (variable.Type.ByteCount > 1 && variableAssignment.Offset == offset + 1) {
                        remove = true;
                    }
                    else if (variableAssignment.Variable.Type.ByteCount > 1 && offset == variableAssignment.Offset + 1) {
                        remove = true;
                    }
                }
                if (!remove)
                    continue;
                RegisterAssignments.Remove(key);
                break;
            }
        }

        public void RemoveStaticVariableAssignments()
        {
            var removedRegisters = new HashSet<Register>();
            foreach (var (key, assignment) in RegisterAssignments) {
                if (!(assignment is RegisterVariableAssignment variableAssignment)) continue;
                var variable = variableAssignment.Variable;
                if (variable.Static) {
                    removedRegisters.Add(key);
                }
            }
            foreach (var assignment in removedRegisters) {
                RegisterAssignments.Remove(assignment);
            }
        }

        public bool IsRegisterInVariableRange(Register? register, Variable? excludedVariable)
        {
            if (register == null)
                return false;
            foreach (var otherVariable in Function.AllVariables) {
                if (otherVariable.Register == null || otherVariable.Equals(excludedVariable))
                    continue;
                var conflicts = otherVariable.Register.Conflicts(register);
                if (!conflicts && !Equals(otherVariable.Register, register))
                    continue;
                var usages = otherVariable.Usages;
                //if (usages.Count == 1) {
                //    var (address, usage) = usages.First();
                //    if (address == Address && usage == Variable.Usage.Read) {
                //        continue;
                //    }
                //}
                if (usages.First().Key <= Address && usages.Last().Key >= Address)
                    return true;
            }
            return false;
        }

        public void BuildResultVariables()
        {
            var variableRegisters = new HashSet<Register>();
            var constantRegisters = new HashSet<Register>();
            foreach (var previousInstruction in PreviousInstructions) {
                foreach (var (register, assignment) in previousInstruction.RegisterAssignments) {
                    switch (assignment) {
                        case RegisterVariableAssignment variableAssignment: {
                                if (!previousInstruction.IsRegisterInVariableRange(register, variableAssignment.Variable)) {
                                    variableRegisters.Add(register);
                                }
                                break;
                            }
                        case RegisterConstantAssignment constantAssignment:
                            if (!previousInstruction.IsRegisterInVariableRange(register, null)) {
                                constantRegisters.Add(register);
                            }

                            break;
                    }
                }
            }
            foreach (var register in variableRegisters) {
                var usages = PreviousInstructions.Select(i => i.RegisterAssignments.TryGetValue(register, out var usage) ? usage : null).Distinct().ToList();
                if (usages.Count != 1)
                    continue;
                {
                    var usage = usages[0];
                    if (usage != null) {
                        RegisterAssignments[register] = usage;
                    }
                }
            }
            foreach (var register in constantRegisters) {
                Constant? value = null;
                foreach (var previousInstruction in PreviousInstructions) {
                    if (previousInstruction.RegisterAssignments.TryGetValue(register, out var assignment)) {
                        if (assignment is RegisterConstantAssignment constantAssignment) {
                            if (value == null) {
                                value = constantAssignment.Constant;
                            }
                            else if (constantAssignment.Constant != value) {
                                goto nextRegister;
                            }
                        }
                        else goto nextRegister;
                    }
                    else goto nextRegister;
                }
                if (value != null) {
                    RegisterAssignments[register] = new RegisterConstantAssignment(value);
                }
            nextRegister:;
            }
        }


        public virtual bool CanAllocateRegister(Variable variable, Register register) => true;


        public Register? PreviousRegisterId(Variable variable, int offset)
        {
            var registerId = GetVariableRegister(variable, offset);
            if (registerId != null)
                return registerId;
            var registerIds = PreviousInstructions.Select(i => i.GetVariableRegister(variable, offset))
                .Distinct().ToList();
            if (registerIds.Count == 1) {
                registerId = registerIds.First()!;
            }
            return registerId;
        }

        public abstract void BuildAssembly();

        public void WriteLine(string code)
        {
            Codes.Add(code);
        }

        public abstract void ReserveOperandRegisters();

        protected void ReserveOperandRegister(Operand operand)
        {
            var variableRegister = operand switch
            {
                VariableOperand variableOperand => variableOperand.Variable.Register,
                IndirectOperand indirectOperand => indirectOperand.Variable.Register,
                _ => null
            };
            if (variableRegister == null) return;
            var reservation = ReserveRegister(variableRegister, operand);
            operandRegisterReservations.Add(reservation);
        }

        public RegisterReservation ReserveRegister(Register register)
        {
            return ReserveRegister(register, null);
        }

        public RegisterReservation ReserveRegister(Register register, Operand? operand)
        {
            var reservation = new RegisterReservation(register, operand, this);
            registerReservations.Add(reservation);
            return reservation;
        }

        public virtual bool IsRegisterReserved(Register register)
        {
            return IsRegisterReserved(register, null);
        }

        public virtual bool IsRegisterReserved(Register register, Operand? excludedOperand)
        {
            bool IsReserved(Register register1)
            {
                return registerReservations.Any(reservation =>
                {
                    if (!Equals(reservation.Register, register)) return false;
                    switch (excludedOperand) {
                        case VariableOperand variableOperand when Equals(variableOperand.Variable, reservation.Variable):
                        case IndirectOperand indirectOperand when Equals(indirectOperand.Variable, reservation.Variable):
                            return false;
                        default:
                            return true;
                    }
                });
            }

            if (IsReserved(register)) return true;

            return register switch
            {
                ByteRegister byteRegister => registerReservations.Any(reservation =>
                {
                    return reservation.Register switch
                    {
                        WordRegister wordRegister => wordRegister.Contains(byteRegister),
                        PointerRegister pointerRegister => pointerRegister.WordRegister != null &&
                                                           pointerRegister.WordRegister.Contains(byteRegister),
                        _ => false
                    };
                }),
                WordRegister wordRegister => registerReservations.Any(reservation =>
                {
                    return reservation.Register switch
                    {
                        ByteRegister byteRegister => wordRegister.Contains(byteRegister),
                        PointerRegister pointerRegister => Equals(pointerRegister.WordRegister, wordRegister),
                        _ => false
                    };
                }),
                PointerRegister pointerRegister => registerReservations.Any(reservation =>
                {
                    return reservation.Register switch
                    {
                        ByteRegister byteRegister => pointerRegister.WordRegister != null &&
                                                     pointerRegister.WordRegister.Contains(byteRegister),
                        WordRegister wordRegister => Equals(pointerRegister.WordRegister, wordRegister),
                        _ => false
                    };
                }),
                _ => false
            };
        }


        public bool CancelOperandRegister(Operand operand)
        {
            for (var i = registerReservations.Count - 1; i >= 0; --i) {
                var reservation = registerReservations[i];
                if (reservation.Variable == null) continue;
                var operandVariable = operand switch
                {
                    VariableOperand variableOperand => variableOperand.Variable,
                    IndirectOperand indirectOperand => indirectOperand.Variable,
                    _ => null
                };
                if (operandVariable == null || !Equals(operandVariable.Register, reservation.Register)) continue;
                registerReservations.RemoveAt(i);
                operandRegisterReservations.Remove(reservation);
                return true;
            }
            return false;
        }

        public bool CancelOperandRegister(Operand operand, Register register)
        {
            for (var i = registerReservations.Count - 1; i >= 0; --i) {
                var reservation = registerReservations[i];
                if (reservation.Variable == null) continue;
                var operandVariable = operand switch
                {
                    VariableOperand variableOperand => variableOperand.Variable,
                    IndirectOperand indirectOperand => indirectOperand.Variable,
                    _ => null
                };
                if (operandVariable == null || !Equals(register, reservation.Register)) continue;
                registerReservations.RemoveAt(i);
                operandRegisterReservations.Remove(reservation);
                return true;
            }
            return false;
        }

        internal bool CancelRegister(Register register)
        {
            Debug.Assert(IsRegisterReserved(register, null));
            for (var i = registerReservations.Count - 1; i >= 0; --i) {
                if (!registerReservations[i].Register.Equals(register)) continue;
                registerReservations.RemoveAt(i);
                return true;
            }
            return false;
        }


        public void WriteAssembly(StreamWriter writer, int tabCount)
        {
            foreach (var code in Codes) {
                WriteTabs(writer, tabCount);
                writer.WriteLine(code);
            }
        }

        public void WriteJumpLine(string line)
        {
            codesToJump.Add(line);
        }

        public void WriteAssemblyAfterRestoring(StreamWriter writer, int tabCount)
        {
            foreach (var line in codesToJump) {
                WriteTabs(writer, tabCount);
                writer.WriteLine(line);
            }
        }


        public static void WriteTabs(StreamWriter writer, int tabCount)
        {
            for (var i = 0; i < tabCount; ++i) {
                writer.Write('\t');
            }
        }

        public void AddSavingVariable(Variable variable)
        {
            SavingVariables.Add(variable);
            if (variable.Register == null)
                return;
            RegisterAssignments.Remove(variable.Register);
        }

        public abstract bool IsSourceOperand(Variable variable);

        public static void Repeat(Action action, int count)
        {
            for (var i = 0; i < count; ++i) {
                action();
            }
        }

        public virtual bool IsResultChanged() => false;

        public virtual bool IsCalling() => false;

        public bool IsConstantAssigned(Register register, int value)
        {
            return RegisterAssignments.TryGetValue(register, out var assignment) && assignment is RegisterConstantAssignment { Constant: ConstantInteger integer } && integer.IntegerValue == value;
        }

        public void SetRegisterConstant(Register register, int value)
        {
            RemoveRegisterAssignment(register);
            RegisterAssignments[register] = new RegisterConstantAssignment(value);
        }

        public bool IsConstantAssigned(Register register, PointerOperand pointerOperand)
        {
            var variable = pointerOperand.Variable;
            var offset = pointerOperand.Offset;
            return IsConstantAssigned(register, variable, offset);
        }

        public bool IsConstantAssigned(Register register, Variable variable, int offset)
        {
            if (!RegisterAssignments.TryGetValue(register, out var assignment)) return false;
            if (assignment is not RegisterConstantAssignment { Constant: ConstantPointer constantPointer }) return false;
            return constantPointer.Variable == variable && constantPointer.Offset == offset;
        }

        public void SetRegisterConstant(Register register, PointerOperand pointerOperand)
        {
            var type = pointerOperand.Type;
            var variable = pointerOperand.Variable;
            var offset = pointerOperand.Offset;
            SetRegisterConstant(register, (PointerType)type, variable, offset);
        }

        public void SetRegisterConstant(Register register, PointerType type, Variable variable, int offset)
        {
            RegisterAssignments[register] =
                new RegisterConstantAssignment(new ConstantPointer(type, variable, offset));
        }

        public int GetRegisterOffset(WordRegister register)
        {
            if (RegisterOffsets.TryGetValue(register, out var offset)) {
                return offset;
            }
            return 0;
        }

        public void SetRegisterOffset(WordRegister register, int offset)
        {
            RegisterOffsets[register] = offset;
            //foreach (var pair in RegisterAssignments) {
            //    var registerAssignment = pair.Value;
            //    if (!Equals(pair.Key, register) || registerAssignment.Offset == offset) continue;
            //    RegisterAssignments[register] = new RegisterAssignment(registerAssignment.Variable, offset);
            //}
        }

        public bool IsRegisterOffsetEmpty()
        {
            foreach (var (key, offset) in RegisterOffsets) {
                if (!changedRegisters.Contains(key)) continue;
                if (offset != 0) return false;
            }
            return true;
        }

        public void AddChanged(Register register)
        {
            if (register is WordRegister wordRegister) {
                if (wordRegister.IsPair()) {
                    Debug.Assert(wordRegister is { Low: { }, High: { } });
                    AddChanged(wordRegister.Low);
                    changedRegisters.Add(wordRegister.High);
                    return;
                }
            }
            changedRegisters.Add(register);
        }

        public ISet<Register> ChangedRegisters() => changedRegisters;

        public void RemoveChanged(Register register)
        {
            if (register is WordRegister wordRegister && wordRegister.IsPair()) {
                Debug.Assert(wordRegister is { Low: { }, High: { } });
                changedRegisters.Remove(wordRegister.Low);
                changedRegisters.Remove(wordRegister.High);
            }
            if (register is PointerRegister { WordRegister: { } } pointerRegister && pointerRegister.WordRegister.IsPair()) {
                Debug.Assert(pointerRegister.WordRegister is { Low: { }, High: { } });
                changedRegisters.Remove(pointerRegister.WordRegister.Low);
                changedRegisters.Remove(pointerRegister.WordRegister.High);
            }
            //else {
            changedRegisters.Remove(register);
            //}
        }

        public bool IsChanged(Register register)
        {
            if (register is WordRegister wordRegister && wordRegister.IsPair()) {
                Debug.Assert(wordRegister is { Low: { }, High: { } });
                return changedRegisters.Contains(wordRegister.Low) || changedRegisters.Contains(wordRegister.High);
            }
            return changedRegisters.Contains(register);
        }
    }
}