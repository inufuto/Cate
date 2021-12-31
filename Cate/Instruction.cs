using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

        public class RegisterAssignment
        {
            public readonly Variable Variable;
            public readonly int Offset;

            public RegisterAssignment(Variable variable, int offset)
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
                if (obj is RegisterAssignment o) {
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
        private readonly ISet<Register> sourceRegisterIds = new HashSet<Register>();
        private readonly ISet<Register> destinationRegisterIds = new HashSet<Register>();
        private readonly ISet<Register> temporaryRegisters = new HashSet<Register>();
        public readonly ISet<Register> ChangedRegisters = new HashSet<Register>();
        //private readonly IDictionary<Register, bool> temporaryRegisterUsages = new Dictionary<Register, bool>();
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


        public ISet<Register> TemporaryRegisterIds
        {
            get
            {
                var temporaryRegisterIds = new HashSet<Register>();
                IEnumerable<Register> changedRegisters = ResultRegister != null ? ChangedRegisters.Where(r => !Equals(r, ResultRegister)) : ChangedRegisters;

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

        public virtual Operand? ResultOperand => null;

        public Register? GetVariableRegister(Variable variable, int offset)
        {
            if (offset == 0 && variable.Register != null) {
                return variable.Register;
            }
            foreach (var (registerId, registerAssignment) in RegisterAssignments) {
                if (
                    registerAssignment.Variable.SameStorage(variable) &&
                    registerAssignment.Offset == offset
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
            if (!RegisterAssignments.TryGetValue(register, out var registerAssignment)) return false;
            return registerAssignment.Variable.SameStorage(variable) && registerAssignment.Offset == offset;
        }

        public bool VariableRegisterMatches(VariableOperand operand, Register register)
        {
            return VariableRegisterMatches(operand.Variable, operand.Offset, register);
        }

        public void SetVariableRegister(Operand operand, Register? register)
        {
            if (!(operand is VariableOperand variableOperand)) {
                if (register != null) {
                    RemoveVariableRegister(register);
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
                    RemoveVariableRegister(register);
                }
                return;
            }
            var registerAssignment = new RegisterAssignment(variable, offset);
            if (register == null) return;
            RegisterAssignments[register] = registerAssignment;
            if (!(register is WordRegister wordRegister)) return;
            if (wordRegister.Low != null) {
                RegisterAssignments.Remove(wordRegister.Low);
                //RemoveVariableRegister(wordRegister.Low);
            }
            if (wordRegister.High != null) {
                RegisterAssignments.Remove(wordRegister.High);
                //RemoveVariableRegister(wordRegister.High);
            }
        }

        public void RemoveVariableRegister(Register register)
        {
            RegisterAssignments.Remove(register);
            foreach (var usage in RegisterAssignments) {
                var key = usage.Key;
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
            foreach (var (key, registerAssignment) in RegisterAssignments) {
                var remove = false;
                if (registerAssignment.Variable.SameStorage(variable)) {
                    if (registerAssignment.Offset == offset) {
                        remove = true;
                    }
                    else if (variable.Type.ByteCount > 1 && registerAssignment.Offset == offset + 1) {
                        remove = true;
                    }
                    else if (registerAssignment.Variable.Type.ByteCount > 1 && offset == registerAssignment.Offset + 1) {
                        remove = true;
                    }
                }
                if (!remove)
                    continue;
                RegisterAssignments.Remove(key);
                break;
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
            var registers = new HashSet<Register>();
            foreach (var previousInstruction in PreviousInstructions) {
                foreach (var (register, registerAssignment) in previousInstruction.RegisterAssignments) {
                    if (previousInstruction.IsRegisterInVariableRange(register, registerAssignment.Variable)) {
                        registers.Add(register);
                    }
                }

                foreach (var registerId in registers) {
                    var usages = PreviousInstructions
                        .Select(i => i.RegisterAssignments.TryGetValue(registerId, out var usage) ? usage : null)
                        .Distinct().ToList();
                    if (usages.Count != 1)
                        continue;
                    {
                        var registerAssignment = usages[0];
                        if (registerAssignment != null) {
                            //SetVariableRegisterId(usage.Variable, usage.Offset, registerId);
                            RegisterAssignments[registerId] = registerAssignment;
                        }
                    }
                }
            }
        }


        public virtual bool CanAllocateRegister(Variable variable, Register register) => true;

        public virtual bool IsRegisterInUse(Register register)
        {
            //if (temporaryRegisters.Contains(register))
            //    return true;
            var pairs = temporaryRegisters.Where(pair => pair.Matches(register)).ToList();
            return pairs.Count > 0 || sourceRegisterIds.Any(id => id.Matches(register));
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
                if (register != null) {
                    sourceRegisterIds.Add(register);
                }
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
            return register != null && sourceRegisterIds.Remove(register);
        }

        public void WriteAssembly(StreamWriter writer, int tabCount)
        {
            foreach (var code in Codes) {
                WriteTabs(writer, tabCount);
                writer.WriteLine(code);
            }
        }

        protected void WriteJumpLine(string line)
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

        public virtual bool IsSourceOperand(Variable variable)
        {
            return variable.Register != null && sourceRegisterIds.Contains(variable.Register);
        }

        public static void Repeat(Action action, int count)
        {
            for (var i = 0; i < count; ++i) {
                action();
            }
        }

        public virtual bool IsResultChanged() => false;

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