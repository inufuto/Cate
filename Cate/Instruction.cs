using System;
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
        public readonly IList<string> Codes = new List<string>();
        private readonly Dictionary<Register, int> sourceRegisters = new Dictionary<Register, int>();
        private readonly ISet<Register> sourceRegisters2 = new HashSet<Register>();
        private readonly ISet<Register> temporaryRegisters = new HashSet<Register>();
        public readonly ISet<Register> ChangedRegisters = new HashSet<Register>();
        public readonly ISet<Variable> SavingVariables = new HashSet<Variable>();
        public readonly Function Function;
        private readonly List<string> codesToJump = new List<string>();
        public readonly ISet<Instruction> PreviousInstructions = new HashSet<Instruction>();
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
                var changedRegisters = ResultRegister != null ? ChangedRegisters.Where(r => !Equals(r, ResultRegister)) : ChangedRegisters;
                foreach (var changedRegister in changedRegisters) {
                    var savingRegisters = Compiler.Instance.SavingRegisters(changedRegister);
                    foreach (var savingRegister in savingRegisters) {
                        temporaryRegisterIds.Add(savingRegister);
                    }
                }
                return temporaryRegisterIds;
            }
        }

        protected virtual Register? ResultRegister => ResultOperand?.Register;

        //public IEnumerable<Register> UsingRegisterIds
        //{
        //    get {
        //        ISet<Register> registers = new HashSet<Register>(destinationRegisterIds);
        //        foreach (var key in temporaryRegisterUsages.Keys) {
        //            registers.Add(key);
        //        }
        //        return registers;
        //    }
        //}

        public virtual Operand? ResultOperand => null;

        public Register? GetVariableRegister(Variable variable, int offset)
        {
            if (offset == 0 && variable.Register != null) {
                return variable.Register;
            }
            foreach (var (registerId, assignment) in RegisterAssignments) {
                if (
                    assignment is RegisterVariableAssignment variableAssignment &&
                    variableAssignment.Variable.SameStorage(variable) &&
                    variableAssignment.Offset == offset
                )
                    return registerId;
            }
            return null;
        }

        public Register? GetVariableRegister(VariableOperand operand)
        {
            return GetVariableRegister(operand.Variable, operand.Offset);
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
            if (!(operand is VariableOperand variableOperand)) {
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

        public virtual bool IsRegisterInUse(Register register)
        {
            //if (temporaryRegisters.Contains(register))
            //    return true;
            var registers = temporaryRegisters.Where(r => r.Matches(register)).ToList();
            return registers.Count > 0 || sourceRegisters.Any(pair => pair.Value > 0 && pair.Key.Matches(register));
        }

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

        public void BeginRegister(Register register)
        {
            Debug.Assert(!temporaryRegisters.Contains(register));
            temporaryRegisters.Add(register);
        }

        public void EndRegister(Register register)
        {
            var removed = temporaryRegisters.Remove(register);
            Debug.Assert(removed);
        }

        public abstract void BuildAssembly();

        public void WriteLine(string code)
        {
            Codes.Add(code);
        }

        public abstract void AddSourceRegisters();

        protected void AddSourceRegister(Operand operand)
        {
            void Add(Variable variable)
            {
                var register = variable.Register;
                if (register == null) return;
                if (sourceRegisters.ContainsKey(register)) {
                    ++sourceRegisters[register];
                }
                else {
                    sourceRegisters[register] = 1;
                }
                sourceRegisters2.Add(register);
            }

            switch (operand) {
                case VariableOperand variableOperand:
                    Add(variableOperand.Variable);
                    break;
                case IndirectOperand indirectOperand:
                    Add(indirectOperand.Variable);
                    break;
            }
        }

        protected static Register? RegisterOfOperand(Operand operand)
        {
            var registerId = operand switch
            {
                VariableOperand variableOperand => variableOperand.Variable.Register,
                IndirectOperand indirectOperand => indirectOperand.Variable.Register,
                _ => null
            };
            return registerId;
        }

        protected bool RemoveSourceRegister(Operand operand)
        {
            var register = RegisterOfOperand(operand);
            if (register == null) return false;
            if (!sourceRegisters.ContainsKey(register)) return false;
            if (--sourceRegisters[register] <= 0) {
                sourceRegisters.Remove(register);
            }
            return true;
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

        public bool IsSourceOperand(Variable variable)
        {
            return variable.Register != null && sourceRegisters2.Contains(variable.Register);
        }

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
            return RegisterAssignments.TryGetValue(register, out var assignment) && assignment is RegisterConstantAssignment { Constant: ConstantPointer constantPointer } && constantPointer.Variable == pointerOperand.Variable && constantPointer.Offset == pointerOperand.Offset;
        }

        public void SetRegisterConstant(Register register, PointerOperand pointerOperand)
        {
            RegisterAssignments[register] = new RegisterConstantAssignment(new ConstantPointer((PointerType)pointerOperand.Type, pointerOperand.Variable, pointerOperand.Offset));
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
                if (!ChangedRegisters.Contains(key)) continue;
                if (offset != 0) return false;
            }
            return true;
        }
    }
}