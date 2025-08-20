using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate;

public abstract class Instruction
{
    [Flags]
    public enum Flag
    {
        Z = 1,
    }


    private class RegisterConstantAssignment(Constant constant)
    {
        public readonly Constant Constant = constant;

        public RegisterConstantAssignment(int value) : this(new ConstantInteger(value)) { }

        public override string ToString()
        {
            return Constant.ToString() ?? string.Empty;
        }
    }


    private class RegisterVariableAssignment(Variable variable, int offset)
    {
        public readonly Variable Variable = variable;
        public readonly int Offset = offset;

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

    private class RegisterCopy
    {
        public readonly Register SourceRegister;

        public RegisterCopy(Register sourceRegister)
        {
            SourceRegister = sourceRegister;
        }
    }

    public readonly int Address;
    public readonly ISet<Instruction> PreviousInstructions = new HashSet<Instruction>();
    public readonly Function Function;
    public readonly IList<string> Codes = new List<string>();
    private readonly List<string> codesToJump = [];
    private readonly IList<RegisterReservation> registerReservations = new List<RegisterReservation>();
    private readonly ISet<Register> changedRegisters = new HashSet<Register>();
    public readonly ISet<Variable> SavingVariables = new HashSet<Variable>();
    private readonly Dictionary<Register, RegisterConstantAssignment> registerConstantAssignments = new();
    private readonly Dictionary<Register, RegisterVariableAssignment> registerVariableAssignments = new();
    private readonly Dictionary<Register, RegisterCopy> registerCopies = new();
    public Flag ResultFlags;
    public readonly IDictionary<WordRegister, int> RegisterOffsets = new Dictionary<WordRegister, int>();

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

        foreach (var (register, assignment) in registerVariableAssignments) {
            if (
                assignment.Variable.SameStorage(variable) &&
                register.ByteCount == variable.FirstType!.ByteCount &&
                assignment.Offset == offset &&
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
            registerVariableAssignments.TryGetValue(register, out var assignment)
        ) {
            return assignment.Variable.SameStorage(variable) && assignment.Offset == offset;
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
            RemoveVariableRegister(variable, offset);
            registerVariableAssignments[register] = resultRegisterUsage;
            if (register is WordRegister wordRegister) {
                if (wordRegister.Low != null) {
                    registerVariableAssignments.Remove(wordRegister.Low);
                    //RemoveRegisterAssignment(wordRegister.Low);
                }
                if (wordRegister.High != null) {
                    registerVariableAssignments.Remove(wordRegister.High);
                    //RemoveRegisterAssignment(wordRegister.High);
                }
            }
        }
    }

    public void SetRegisterCopy(Register register, Register sourceRegister)
    {
        registerCopies[register] = new RegisterCopy(sourceRegister);
    }

    public bool IsRegisterCopy(Register register, Register sourceRegister)
    {
        return registerCopies.TryGetValue(register, out var registerCopy) && registerCopy.SourceRegister.Equals(sourceRegister);
    }


    public void RemoveRegisterAssignment(Register register)
    {
        Remove(registerConstantAssignments);
        Remove(registerVariableAssignments);
        Remove(registerCopies);
        return;

        void Remove<T>(Dictionary<Register, T> dictionary)
        {
            dictionary.Remove(register);
            foreach (var pair in dictionary) {
                var key = pair.Key;
                if (register.Conflicts(key)) {
                    dictionary.Remove(key);
                }
                if (pair.Value is RegisterCopy registerCopy && registerCopy.SourceRegister.Conflicts(register)) {
                    dictionary.Remove(key);
                }
            }
        }
    }

    public void RemoveVariableRegister(Operand operand)
    {
        if (operand is not VariableOperand variableOperand) return;
        var variable = variableOperand.Variable;
        var offset = variableOperand.Offset;
        RemoveVariableRegister(variable, offset);
    }

    public void RemoveVariableRegister(Variable variable, int offset)
    {
        foreach (var (key, assignment) in registerVariableAssignments) {
            var remove = false;
            if (assignment.Variable.SameStorage(variable)) {
                if (assignment.Offset == offset) {
                    remove = true;
                }
                else if (variable.Type.ByteCount > 1 && assignment.Offset == offset + 1) {
                    remove = true;
                }
                else if (assignment.Variable.Type.ByteCount > 1 && offset == assignment.Offset + 1) {
                    remove = true;
                }
            }
            if (!remove)
                continue;
            registerVariableAssignments.Remove(key);
            break;
        }
    }

    public void RemoveStaticVariableAssignments()
    {
        var removedRegisters = new HashSet<Register>();
        foreach (var (key, assignment) in registerVariableAssignments) {
            var variable = assignment.Variable;
            if (variable.Static) {
                removedRegisters.Add(key);
            }
        }
        foreach (var assignment in removedRegisters) {
            registerVariableAssignments.Remove(assignment);
        }
    }

    public bool IsRegisterAssigned(Register register)
    {
        return
            registerConstantAssignments.Any(a => Equals(a.Key, register)) ||
            registerVariableAssignments.Any(a => Equals(a.Key, register)) ||
            registerCopies.Any(a => Equals(a.Key, register));
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
            if (usages.First().Key <= Address && usages.Last().Key >= Address)
                return true;
        }
        return false;
    }

    public void BuildResultVariables()
    {
        var variableRegisters = new HashSet<Register>();
        var constantRegisters = new HashSet<Register>();
        var copies = new HashSet<Register>();
        foreach (var previousInstruction in PreviousInstructions) {
            foreach (var (register, _) in previousInstruction.registerConstantAssignments) {
                if (!previousInstruction.IsRegisterInVariableRange(register, null)) {
                    constantRegisters.Add(register);
                }
            }
            foreach (var (register, assignment) in previousInstruction.registerVariableAssignments) {
                if (!previousInstruction.IsRegisterInVariableRange(register, assignment.Variable)) {
                    variableRegisters.Add(register);
                }
            }
            foreach (var (register, assignment) in previousInstruction.registerCopies) {
                if (!previousInstruction.IsRegisterInVariableRange(register, null)) {
                    copies.Add(register);
                }
            }
        }
        foreach (var register in variableRegisters) {
            var usages = PreviousInstructions.Select(i => i.registerVariableAssignments.GetValueOrDefault(register)).Distinct().ToList();
            if (usages.Count != 1)
                continue;
            {
                var usage = usages[0];
                if (usage != null) {
                    registerVariableAssignments[register] = usage;
                }
            }
        }
        foreach (var register in constantRegisters) {
            Constant? value = null;
            foreach (var previousInstruction in PreviousInstructions) {
                if (previousInstruction.registerConstantAssignments.TryGetValue(register, out var assignment)) {
                    if (value == null) {
                        value = assignment.Constant;
                    }
                    else if (assignment.Constant != value) {
                        goto nextRegister;
                    }
                }
                else goto nextRegister;
            }
            if (value != null) {
                registerConstantAssignments[register] = new RegisterConstantAssignment(value);
            }
        nextRegister:;
        }

        foreach (var register in copies) {
            Register? sourceRegister = null;
            foreach (var previousInstruction in PreviousInstructions) {
                if (previousInstruction.registerCopies.TryGetValue(register, out var registerCopy)) {
                    if (sourceRegister == null) {
                        sourceRegister = registerCopy.SourceRegister;
                    }
                    else if (!Equals(registerCopy.SourceRegister, sourceRegister)) {
                        goto nextRegister;
                    }
                }
                else goto nextRegister;
            }
            if (sourceRegister != null) {
                registerCopies[register] = new RegisterCopy(sourceRegister);
            }
        nextRegister:;
        }

        Compiler.RemoveRegisterAssignment(this);
    }


    public virtual int? RegisterAdaptability(Variable variable, Register register) => 0;


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
        ReserveRegister(variableRegister, operand);
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
            ByteRegister byteRegister => registerReservations.Any(reservation => reservation.Register is WordRegister wordRegister && wordRegister.Contains(byteRegister)),
            WordRegister wordRegister => registerReservations.Any(reservation =>
            {
                if (reservation.Register is ByteRegister byteRegister)
                    return wordRegister.Contains(byteRegister);
                return false;
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
            //operandRegisterReservations.Remove(reservation);
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
            //operandRegisterReservations.Remove(reservation);
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
        RemoveRegisterAssignment(variable.Register);
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
        return registerConstantAssignments.TryGetValue(register, out var assignment) && assignment is { Constant: ConstantInteger integer } && integer.IntegerValue == value;
    }

    public void SetRegisterConstant(Register register, int value)
    {
        RemoveRegisterAssignment(register);
        registerConstantAssignments[register] = new RegisterConstantAssignment(value);
    }

    public bool IsConstantAssigned(Register register, PointerOperand pointerOperand)
    {
        var variable = pointerOperand.Variable;
        var offset = pointerOperand.Offset;
        return IsConstantAssigned(register, variable, offset);
    }

    public bool IsConstantAssigned(Register register, Variable variable, int offset)
    {
        if (!registerConstantAssignments.TryGetValue(register, out var assignment)) return false;
        if (assignment is not { Constant: ConstantPointer constantPointer }) return false;
        return constantPointer.Variable == variable && constantPointer.Offset == offset;
    }

    public void SetRegisterConstant(Register register, PointerOperand pointerOperand)
    {
        var type = pointerOperand.Type;
        if (type is IntegerType integerType) {
            type = new PointerType(new IntegerType(1, false));
        }
        var variable = pointerOperand.Variable;
        var offset = pointerOperand.Offset;
        SetRegisterConstant(register, (PointerType)type, variable, offset);
    }

    public void SetRegisterConstant(Register register, PointerType type, Variable variable, int offset)
    {
        registerConstantAssignments[register] =
            new RegisterConstantAssignment(new ConstantPointer(type, variable, offset));
    }

    //public int GetRegisterOffset(WordRegister register)
    //{
    //    if (RegisterOffsets.TryGetValue(register, out var offset)) {
    //        return offset;
    //    }
    //    return 0;
    //}

    public void SetRegisterOffset(WordRegister register, int offset)
    {
        RegisterOffsets[register] = offset;
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
        changedRegisters.Remove(register);
    }

    public bool IsChanged(Register register)
    {
        if (register is WordRegister wordRegister && wordRegister.IsPair()) {
            Debug.Assert(wordRegister is { Low: { }, High: { } });
            return changedRegisters.Contains(wordRegister.Low) || changedRegisters.Contains(wordRegister.High);
        }
        return changedRegisters.Contains(register);
    }


    public bool IsRegisterDestination(Register register) => ResultOperand != null && ResultOperand.Conflicts(register);

    public bool IsRegisterSource(Register register) => SourceOperands.Any(o => o.Conflicts(register));

    public abstract List<Operand> SourceOperands { get; }
}