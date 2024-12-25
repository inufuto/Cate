using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Inu.Cate;

public abstract class SubroutineInstruction : Instruction
{
    protected class ParameterAssignment(Function.Parameter parameter, Operand operand)
    {
        public readonly Function.Parameter Parameter = parameter;
        public readonly Operand Operand = operand;

        public RegisterReservation? RegisterReservation { get; set; }

        public bool Done { get; private set; }

        public override string ToString()
        {
            return "{" + Parameter.Register + "," + Operand + "," + RegisterReservation + "," + Done + "}";
        }

        public void SetDone(SubroutineInstruction instruction, Register? register)
        {
            //instruction.CancelOperandRegister(Operand);
            if (register != null) { // && !Equals(register, Parameter.Register)
                RegisterReservation ??= instruction.ReserveRegister(register);
            }
            Done = true;
        }

        public void Exchange(SubroutineInstruction instruction, ParameterAssignment other)
        {
            Debug.Assert(other.Parameter.Register != null);
            switch (Parameter.Register) {
                case ByteRegister byteRegister:
                    byteRegister.Exchange(instruction, (ByteRegister)other.Parameter.Register);
                    break;
                case WordRegister wordRegister:
                    wordRegister.Exchange(instruction, (WordRegister)other.Parameter.Register);
                    break;
            }
            Done = true;
            other.Done = true;
        }

        public RegisterReservation? Close(Instruction instruction)
        {
            Debug.Assert(Done);
            if (RegisterReservation == null)
                return null;
            RegisterReservation? newReservation = null;
            if (!Equals(RegisterReservation.Register, Parameter.Register)) {
                Debug.Assert(Parameter.Register != null);
#if DEBUG
                instruction.WriteLine("\t;[Close] " + Parameter.Register + " <- " + RegisterReservation.Register);
#endif
                switch (Parameter.Register) {
                    case ByteRegister byteRegister:
                        newReservation = instruction.ByteOperation.ReserveRegister(instruction, byteRegister);
                        byteRegister.CopyFrom(instruction, RegisterReservation.ByteRegister);
                        break;
                    case WordRegister wordRegister:
                        newReservation = instruction.WordOperation.ReserveRegister(instruction, wordRegister);
                        wordRegister.CopyFrom(instruction, RegisterReservation.WordRegister);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            RegisterReservation = null;
            Done = false;
            return newReservation;
        }
    }

    public readonly Function TargetFunction;
    public readonly AssignableOperand? DestinationOperand;
    public readonly List<Operand> SourceOperands;

    protected readonly List<ParameterAssignment> ParameterAssignments = [];

    public override bool IsSourceOperand(Variable variable)
    {
        return SourceOperands.Any(sourceOperand => sourceOperand.IsVariable(variable));
    }

    public override bool IsResultChanged()
    {
        if (ResultOperand == null) {
            return false;
        }
        var returnRegister = Compiler.Instance.ReturnRegister((ParameterizableType)TargetFunction.Type);
        return TargetFunction.Parameters.Any(parameter => parameter.Register != null && parameter.Register.Conflicts(returnRegister));
    }

    public override bool IsCalling() => true;

    protected SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand,
        List<Operand> sourceOperands) : base(function)
    {
        TargetFunction = targetFunction;
        DestinationOperand = destinationOperand;
        SourceOperands = sourceOperands;

        DestinationOperand?.AddUsage(function.NextAddress, Variable.Usage.Write);
        for (var i = 0; i < SourceOperands.Count; i++) {
            var sourceOperand = SourceOperands[i];
            sourceOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
            ParameterAssignments.Add(new ParameterAssignment(targetFunction.Parameters[i], sourceOperands[i]));
        }
    }

    public override string ToString()
    {
        var s = new StringBuilder();
        if (DestinationOperand != null) {
            s.Append(DestinationOperand.ToString());
            s.Append(" = ");
        }
        s.Append(TargetFunction.Label);
        s.Append('(');
        var index = 0;
        foreach (var sourceOperand in SourceOperands) {
            s.Append(sourceOperand.ToString());
            if (++index < TargetFunction.Parameters.Count) {
                s.Append(',');
            }
        }

        s.Append(')');
        return s.ToString();
    }

    public override void BuildAssembly()
    {
        var returnRegister = Compiler.Instance.ReturnRegister((ParameterizableType)TargetFunction.Type);
        RegisterReservation? alternative = null;
        {
            var reservations = FillParameters();
            ResultFlags = 0;
            Call();
            returnRegister = ResolveReturnRegister(reservations, returnRegister, ref alternative);
        }
        RemoveStaticVariableAssignments();

        if (returnRegister != null) {
            AddChanged(returnRegister);
        }

        if (DestinationOperand != null) {
            Debug.Assert(returnRegister != null);
            RemoveRegisterAssignment(returnRegister);
            var resultSaved = !IsRegisterReserved(returnRegister);
            if (resultSaved) {
                ReserveRegister(returnRegister);
            }

            RemoveRegisterAssignment(returnRegister);
            switch (returnRegister) {
                case ByteRegister byteRegister:
                    byteRegister.Store(this, DestinationOperand);
                    break;
                case WordRegister wordRegister:
                    wordRegister.Store(this, DestinationOperand);
                    break;
            }
        }
        alternative?.Dispose();
    }

    protected virtual Register? ResolveReturnRegister(List<RegisterReservation> reservations, Register? returnRegister,
        ref RegisterReservation? alternative)
    {
        foreach (var reservation in reservations) {
            if (reservation.Register.Conflicts(returnRegister)) {
                switch (reservation.Register) {
                    case ByteRegister:
                        alternative = ByteOperation.ReserveAnyRegister(this);
                        alternative.ByteRegister.CopyFrom(this, reservation.ByteRegister);
                        returnRegister = alternative.ByteRegister;
                        break;
                    case WordRegister:
                        alternative = WordOperation.ReserveAnyRegister(this);
                        alternative.WordRegister.CopyFrom(this, reservation.WordRegister);
                        returnRegister = alternative.WordRegister;
                        break;
                }
            }
            reservation.Dispose();
        }

        return returnRegister;
    }


    public override void ReserveOperandRegisters()
    {
        if (DestinationOperand is IndirectOperand indirectOperand) {
            ReserveOperandRegister(indirectOperand);
        }
        foreach (var sourceOperand in SourceOperands) {
            ReserveOperandRegister(sourceOperand);
        }
    }

    protected Dictionary<Function.Parameter, Operand> OperandPairs()
    {
        var pairs = new Dictionary<Function.Parameter, Operand>();
        for (var index = SourceOperands.Count - 1; index >= 0; --index) {
            pairs[TargetFunction.Parameters[index]] = SourceOperands[index];
        }
        return pairs;
    }

    public override Operand? ResultOperand => DestinationOperand;

    protected abstract void Call();

    protected List<RegisterReservation> FillParameters()
    {
        StoreParameters();

        var firstRegister = Compiler.ParameterRegister(0, IntegerType.ByteType);
        var changed = true;
        while (changed) {
            changed = false;

            // same register
            for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                var assignment = ParameterAssignments[i];
                if (assignment.Done) continue;
                // the source register and the parameter register match
                var parameter = assignment.Parameter;
                var operand = assignment.Operand;
                Debug.Assert(parameter.Register != null);
                if (operand is not VariableOperand variableOperand) continue;
                var register = GetVariableRegister(variableOperand.Variable, variableOperand.Offset, r => r.Equals(parameter.Register));
                if (!Equals(register, parameter.Register)) continue;
                assignment.RegisterReservation = ReserveRegister(register, operand);
                Load(parameter.Register, operand);
                assignment.SetDone(this, register);
                changed = true;
            }
            if (changed) continue;

            for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                var assignment = ParameterAssignments[i];
                if (assignment.Done) continue;
                if (Equals(assignment.Parameter.Register, firstRegister)) continue;
                // load straight
                var parameter = assignment.Parameter;
                var operand = assignment.Operand;
                var register = parameter.Register;
                Debug.Assert(register != null);
                if (IsRegisterReserved(register) || IsSourceVariable(register)) continue;
                assignment.RegisterReservation = ReserveRegister(register, operand);
                Load(register, operand);
                assignment.SetDone(this, register);
                changed = true;
            }
            if (changed) continue;

            for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                var assignment = ParameterAssignments[i];
                if (assignment.Done) continue;
                if (Equals(assignment.Parameter.Register, firstRegister)) continue;
                var parameter = assignment.Parameter;
                var operand = assignment.Operand;
                {
                    Register? register;
                    if (parameter.Type.ByteCount == 1) {
                        register = ByteOperation.Registers.Find(r => !Equals(r, firstRegister) && !IsRegisterReserved(r));
                    }
                    else {
                        register = WordOperation.Registers.Find(r => !Equals(r, firstRegister) && !IsRegisterReserved(r));
                    }
                    if (register == null || Equals(register, firstRegister)) continue;
                    if (parameter.Register != null) RemoveRegisterAssignment(parameter.Register);
                    assignment.RegisterReservation = ReserveRegister(register);
                    Load(register, operand);
                    assignment.SetDone(this, register);
                    changed = true;
                }
            }
            if (changed) continue;
            for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                var assignment = ParameterAssignments[i];
                if (assignment.Done) continue;
                var parameter = assignment.Parameter;
                var operand = assignment.Operand;
                var other = ParameterAssignments.Find(a =>
                {
                    if (a.Done) return false;
                    Register? variableRegister = null;
                    if (a.Operand is not VariableOperand variableOperand)
                        return Equals(variableRegister, parameter.Register);
                    var variable = variableOperand.Variable;
                    variableRegister = GetVariableRegister(variable, variableOperand.Offset);
                    return Equals(variableRegister, parameter.Register);
                });
                if (other != null && Equals(other.Parameter.Register, operand.Register)) {
                    Debug.Assert(other.Parameter.Register != null);
                    Load(other.Parameter.Register, operand);
                    assignment.Exchange(this, other);
                }
            }
            if (changed || firstRegister == null) continue;
            firstRegister = null;
            changed = true;
        }

        List<ParameterAssignment> Twisted()
        {
            return ParameterAssignments.Where(a =>
            {
                if (a.Done) return false;
                if (a is { RegisterReservation: { }, Operand: VariableOperand variableOperand } && Equals(GetVariableRegister(variableOperand), a.RegisterReservation.Register)) return false;
                return a.RegisterReservation != null && !Equals(a.Parameter.Register, a.RegisterReservation.Register);
            }).ToList();
        }
        var twisted = Twisted();
        var parameterReservations = new List<RegisterReservation>();
        while (twisted.Count > 1) {
            var parameterAssignments = twisted.Where(parameterAssignment => !twisted.Any(a => a.RegisterReservation != null && a != parameterAssignment && Equals(a.RegisterReservation.Register, parameterAssignment.Parameter.Register))).ToList();
            if (!parameterAssignments.Any()) break;
            foreach (var parameterAssignment in parameterAssignments) {
                var reservation = parameterAssignment.Close(this);
                if (reservation != null) parameterReservations.Add(reservation);
            }
            twisted = Twisted();
        }
        foreach (var assignment in ParameterAssignments.Where(assignment => assignment.Done)) {
            var reservation = assignment.Close(this);
            if (reservation != null) parameterReservations.Add(reservation);
        }
        return parameterReservations;
    }

    private bool IsSourceVariable(Register register)
    {
        foreach (var parameterAssignment in ParameterAssignments.Where(parameterAssignment => !parameterAssignment.Done && !Equals(parameterAssignment.Parameter.Register, register))) {
            switch (parameterAssignment.Operand) {
                case VariableOperand variableOperand: {

                        var operandRegister = GetVariableRegister(variableOperand.Variable, variableOperand.Offset); //variableOperand.Variable.Register;
                        if (operandRegister != null && operandRegister.Matches(register)) return true;
                        break;
                    }
                case IndirectOperand indirectOperand: {
                        var operandRegister = indirectOperand.Variable.Register;
                        if (operandRegister != null && operandRegister.Matches(register)) return true;
                        break;
                    }
            }
        }
        return false;
    }

    private void Load(Register register, Operand operand)
    {
#if DEBUG
        WriteLine("\t; " + register + " <- {" + operand + "}");
#endif
        switch (register) {
            case ByteRegister byteRegister:
                byteRegister.Load(this, operand);
                break;
            case WordRegister wordRegister:
                wordRegister.Load(this, operand);
                break;
        }
        //CancelOperandRegister(operand);
        if (!Equals(operand.Register, register)) {
            AddChanged(register);
        }
    }

    protected abstract void StoreParameters();

    protected virtual List<ByteRegister> Candidates(Operand operand)
    {
        return ByteOperation.Registers;
    }

    protected void StoreParametersDirect()
    {
        ParameterAssignments.Sort((a1, a2) =>
        {
            var id1 = a1.Operand.Register?.Id ?? int.MaxValue;
            var id2 = a2.Operand.Register?.Id ?? int.MaxValue;
            return id1 - id2;
        });
        foreach (var assignment in ParameterAssignments) {
            if (assignment.Done || assignment.Parameter.Register != null)
                continue;
            var parameter = assignment.Parameter;
            var label = TargetFunction.ParameterLabel(parameter);
            var operand = assignment.Operand;
            if (parameter.Type.ByteCount == 1) {
                if (operand is IntegerOperand { IntegerValue: 0 }) {
                    ByteOperation.ClearByte(this, label);
                }
                else {
                    var candidates = Candidates(operand);
                    using var reservation = ByteOperation.ReserveAnyRegister(this, candidates, operand);
                    reservation.ByteRegister.Load(this, operand);
                    reservation.ByteRegister.StoreToMemory(this, label);
                }
            }
            else {
                if (operand is IntegerOperand { IntegerValue: 0 }) {
                    WordOperation.ClearWord(this, label);
                }
                else {
                    StoreWord(operand, label, parameter.Type);
                }
            }
            assignment.SetDone(this, null);
        }
    }

    protected virtual void StoreWord(Operand operand, string label, ParameterizableType type)
    {
        if (operand is VariableOperand variableOperand) {
            var variableRegister = GetVariableRegister(variableOperand);
            if (variableRegister is WordRegister wordRegister) {
                wordRegister.Load(this, operand);
                wordRegister.StoreToMemory(this, label);
                return;
            }
        }
        using var reservation = WordOperation.ReserveAnyRegister(this, WordOperation.RegistersForType(type));
        reservation.WordRegister.Load(this, operand);
        reservation.WordRegister.StoreToMemory(this, label);
    }

    protected void StoreParametersViaPointer()
    {
        using var pointerReservation = WordOperation.ReserveAnyRegister(this, WordOperation.Registers);
        var pointerRegister = pointerReservation.WordRegister;
        var index = 0;
        var count = ParameterAssignments.Count(a => a.Parameter.Register == null);
        foreach (var assignment in ParameterAssignments.Where(assignment => assignment.Parameter.Register == null)) {
            Debug.Assert(!assignment.Done);
            var parameter = assignment.Parameter;
            var last = index >= count - 1;

            var operand = assignment.Operand;
            if (index == 0) {
                pointerRegister.LoadConstant(this, TargetFunction.ParameterLabel(parameter));
            }

            if (parameter.Type.ByteCount == 1) {
                switch (operand) {
                    case IntegerOperand integerOperand:
                        ByteOperation.StoreConstantIndirect(this, pointerRegister, 0, integerOperand.IntegerValue);
                        if (!last) {
                            pointerRegister.Add(this, 1);
                        }
                        break;
                    case VariableOperand { Variable: { Register: ByteRegister register } variable } variableOperand: {
                            Debug.Assert(variableOperand.Offset == 0);
                            using (ByteOperation.ReserveRegister(this, register, variableOperand)) {
                                StoreByte(register);
                            }
                            break;
                        }
                    case VariableOperand _:
                        using (var reservation = ByteOperation.ReserveAnyRegister(this, ByteOperation.Accumulators)) {
                            reservation.ByteRegister.Load(this, operand);
                            StoreByte(reservation.ByteRegister);
                        }
                        break;
                    default:
                        using (var reservation = ByteOperation.ReserveAnyRegister(this)) {
                            reservation.ByteRegister.Load(this, operand);
                            StoreByte(reservation.ByteRegister);
                        }
                        break;
                }
            }
            else {
                if (operand is IntegerOperand integerOperand) {
                    WordOperation.StoreConstantIndirect(this, pointerRegister, 0, integerOperand.IntegerValue);
                    if (!last) {
                        pointerRegister.Add(this, operand.Type.ByteCount);
                    }
                }
                if (operand.Register is WordRegister wordRegister && wordRegister.IsPair()) {
                    wordRegister.Load(this, operand);
                    StoreViaPointer(pointerRegister, wordRegister, last);
                }
                else {
                    using var reservation = WordOperation.ReserveAnyRegister(this);
                    reservation.WordRegister.Load(this, operand);
                    StoreViaPointer(pointerRegister, reservation.WordRegister, last);
                }
            }
            assignment.SetDone(this, null);
            ++index;
            continue;

            void StoreByte(ByteRegister register)
            {
                register.StoreIndirect(this, pointerRegister, 0);
                if (!last) {
                    pointerRegister.Add(this, 1);
                }
            }
        }
    }

    protected virtual void StoreViaPointer(WordRegister pointerRegister, WordRegister register, bool last)
    {
        register.StoreIndirect(this, pointerRegister, 0);
        if (!last) {
            pointerRegister.Add(this, register.ByteCount);
        }
    }

    public override int? RegisterAdaptability(Variable variable, Register register)
    {
        foreach (var assignment in ParameterAssignments) {
            if (assignment.Operand is not VariableOperand variableOperand ||
                !variableOperand.Variable.Equals(variable)) continue;
            if (Equals(assignment.Parameter.Register, register)) {
                return 1;
            }
        }
        return base.RegisterAdaptability(variable, register);
    }
}