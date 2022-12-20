using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Inu.Cate
{
    public abstract class SubroutineInstruction : Instruction
    {
        protected class ParameterAssignment
        {
            public readonly Function.Parameter Parameter;
            public readonly Operand Operand;
            public Register? Register;
            public bool Done { get; private set; }

            public ParameterAssignment(Function.Parameter parameter, Operand operand)
            {
                Parameter = parameter;
                Operand = operand;
            }

            public override string ToString()
            {
                return "{" + Parameter.Register + "," + Operand + "," + Register + "," + Done + "}";
            }

            public void SetDone(SubroutineInstruction instruction, Register? register)
            {
                Register = register;
                Done = true;
                instruction.RemoveSourceRegister(Operand);
                if (register != null) {
                    instruction.BeginRegister(register);
                }
            }

            public void Exchange(SubroutineInstruction instruction, ParameterAssignment other)
            {
                Debug.Assert(other.Parameter.Register != null);
                if (Parameter.Register is ByteRegister byteRegister) {
                    byteRegister.Exchange(instruction, (ByteRegister)other.Parameter.Register);
                }
                if (Parameter.Register is WordRegister wordRegister) {
                    wordRegister.CopyFrom(instruction, (WordRegister)other.Parameter.Register);
                }
                SetDone(instruction, other.Operand.Register);
                other.SetDone(instruction, Operand.Register);
            }

            public void Close(Instruction instruction)
            {
                Debug.Assert(Done);
                if (Register == null)
                    return;
                if (!Equals(Register, Parameter.Register)) {
                    Debug.Assert(Parameter.Register != null);
                    if (Parameter.Register is ByteRegister byteRegister) {
                        byteRegister.CopyFrom(instruction, (ByteRegister)Register);
                    }
                    if (Parameter.Register is WordRegister wordRegister) {
                        wordRegister.CopyFrom(instruction, (WordRegister)Register);
                    }
                }
                instruction.EndRegister(Register);
                Register = null;
                Done = false;
            }
        }

        public readonly Function TargetFunction;
        public readonly AssignableOperand? DestinationOperand;
        public readonly List<Operand> SourceOperands;

        protected readonly List<ParameterAssignment> ParameterAssignments = new List<ParameterAssignment>();

        public override bool IsResultChanged()
        {
            if (ResultOperand == null) {
                return false;
            }
            var returnRegister = Compiler.Instance.ReturnRegister(TargetFunction.Type.ByteCount);
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

        public override bool IsRegisterInUse(Register register)
        {
            if (base.IsRegisterInUse(register)) return true;
            foreach (var assignment in ParameterAssignments) {
                if (assignment.Done) {
                    if (assignment.Parameter.Register != null && assignment.Parameter.Register.Matches(register)) {
                        return true;
                    }
                }
                else if (assignment.Register != null) {
                    if (assignment.Register.Matches(register)) {
                        return true;
                    }
                }
                else {
                    if (assignment.Operand.Matches(register)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void BuildAssembly()
        {
            Register? savedRegister = null;
            if (DestinationOperand is IndirectOperand destinationIndirectOperand && destinationIndirectOperand.Variable.Register != null) {
                var destinationRegister = destinationIndirectOperand.Variable.Register;
                if (ParameterAssignments.Any(p => p.Parameter.Register != null && p.Parameter.Register.Conflicts(destinationRegister))) {
                    savedRegister = destinationRegister;
                    savedRegister.Save(this);
                }
            }

            FillParameters();
            ResultFlags = 0;
            Call();
            RemoveStaticVariableAssignments();

            var returnRegister = Compiler.Instance.ReturnRegister(TargetFunction.Type.ByteCount);
            if (returnRegister != null) {
                ChangedRegisters.Add(returnRegister);
            }
            if (DestinationOperand == null)
                return;

            void StoreResult(Register rr)
            {
                if (savedRegister != null) {
                    savedRegister.Restore(this);
                    var removedRegisters = ChangedRegisters.Where(r =>
                        Equals(savedRegister, r) || (r is ByteRegister changedByteRegister &&
                                                     Equals(changedByteRegister.PairRegister, savedRegister))).ToList();
                    foreach (var removedRegister in removedRegisters) {
                        ChangedRegisters.Remove(removedRegister);
                    }
                }

                RemoveRegisterAssignment(rr);
                var resultSaved = !IsRegisterInUse(rr);
                if (resultSaved) {
                    BeginRegister(rr);
                }
                RemoveRegisterAssignment(rr);
                switch (rr) {
                    case ByteRegister byteRegister:
                        byteRegister.Store(this, DestinationOperand);
                        break;
                    case WordRegister wordRegister:
                        wordRegister.Store(this, DestinationOperand);
                        break;
                }
                if (resultSaved) {
                    EndRegister(rr);
                }
            }

            Debug.Assert(returnRegister != null);
            if (savedRegister != null && returnRegister.Conflicts(savedRegister)) {
                switch (returnRegister) {
                    case ByteRegister byteRegister: {
                            var candidates = ByteOperation.Registers.Where(r => !IsRegisterInUse(r) && !r.Conflicts(savedRegister)).ToList();
                            ByteOperation.UsingAnyRegister(this, candidates, register =>
                            {
                                register.CopyFrom(this, byteRegister);
                                StoreResult(register);
                            });
                            break;
                        }
                    case WordRegister wordRegister: {
                            var candidates = WordOperation.Registers.Where(r => !IsRegisterInUse(r) && !r.Conflicts(savedRegister)).ToList();
                            WordOperation.UsingAnyRegister(this, candidates, register =>
                            {
                                register.CopyFrom(this, wordRegister);
                                StoreResult(register);
                            });
                            break;
                        }
                }
                return;
            }
            StoreResult(returnRegister);
        }


        public override void AddSourceRegisters()
        {
            foreach (var sourceOperand in SourceOperands) {
                AddSourceRegister(sourceOperand);
            }
            if (DestinationOperand is IndirectOperand indirectOperand && indirectOperand.Variable.Register != null) {
                AddSourceRegister(DestinationOperand);
            }
        }

        //public override void RemoveDestinationRegister()
        //{
        //    if (DestinationOperand != null) {
        //        RemoveChangedRegisters(DestinationOperand);
        //    }
        //}

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

        protected void FillParameters()
        {
            StoreParameters();
            //foreach (var assignment in ParameterAssignments.Where(assignment => !Equals(assignment.Parameter.Register, assignment.Operand.Register))) {
            //    if (assignment.Parameter.Register == null) continue;
            //    if (!(assignment.Operand is VariableOperand variableOperand) ||
            //        !Equals(GetVariableRegister(variableOperand.Variable, variableOperand.Offset), assignment.Parameter.Register)) {
            //        RemoveRegisterAssignment(assignment.Parameter.Register);
            //    }
            //}

            var firstRegister = Compiler.ParameterRegister(0, IntegerType.ByteType);
            var changed = true;
            while (changed) {
                changed = false;
                for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                    var assignment = ParameterAssignments[i];
                    if (assignment.Done)
                        continue;

                    // the source register and the parameter register match
                    var parameter = assignment.Parameter;
                    var operand = assignment.Operand;
                    Debug.Assert(parameter.Register != null);
                    if (!(operand is VariableOperand variableOperand)) continue;
                    var register = variableOperand.Variable.Register
                                   ?? GetVariableRegister(variableOperand.Variable, variableOperand.Offset);
                    if (!Equals(register, parameter.Register)) continue;
                    assignment.SetDone(this, register);
                    changed = true;
                }
                if (changed)
                    continue;
                for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                    var assignment = ParameterAssignments[i];
                    if (assignment.Done) continue;
                    if (Equals(assignment.Parameter.Register, firstRegister)) continue;
                    // load straight
                    var parameter = assignment.Parameter;
                    var operand = assignment.Operand;
                    Debug.Assert(parameter.Register != null);
                    if (IsRegisterInUse(parameter.Register) || IsSourceVariable(parameter.Register)) continue;
                    assignment.Register = parameter.Register;
                    Load(parameter.Register, operand);
                    assignment.SetDone(this, parameter.Register);
                    changed = true;
                }
                if (changed)
                    continue;
                for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                    var assignment = ParameterAssignments[i];
                    if (assignment.Done) continue;
                    if (Equals(assignment.Parameter.Register, firstRegister)) continue;
                    var parameter = assignment.Parameter;
                    var operand = assignment.Operand;
                    {
                        Register? register;
                        if (parameter.Type.ByteCount == 1) {
                            register = ByteOperation.Registers.Find(r => !IsRegisterInUse(r));
                        }
                        else {
                            register = WordOperation.Registers.Find(r => !IsRegisterInUse(r));
                        }
                        if (register == null || Equals(register, firstRegister)) continue;
                        if (parameter.Register != null) RemoveRegisterAssignment(parameter.Register);
                        assignment.Register = register;
                        Load(register, operand);
                        assignment.SetDone(this, register);
                        changed = true;
                    }
                }
                if (changed)
                    continue;
                for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                    var assignment = ParameterAssignments[i];
                    if (assignment.Done)
                        continue;
                    var parameter = assignment.Parameter;
                    var operand = assignment.Operand;
                    var other = ParameterAssignments.Find(a =>
                    {
                        if (a.Done) return false;
                        Register? variableRegister = null;
                        if (a.Operand is VariableOperand variableOperand) {
                            var variable = variableOperand.Variable;
                            variableRegister = GetVariableRegister(variable, variableOperand.Offset);
                        }
                        return Equals(variableRegister, parameter.Register);
                    });
                    if (other != null && Equals(other.Parameter.Register, operand.Register)) {
                        assignment.Exchange(this, other);
                    }
                }
                if (!changed && firstRegister != null) {
                    firstRegister = null;
                    changed = true;
                }
            }

            List<ParameterAssignment> Twisted()
            {
                return ParameterAssignments.Where(a =>
                {
                    if (!a.Done) return false;
                    if (a.Operand is VariableOperand variableOperand && Equals(GetVariableRegister(variableOperand), a.Register)) return false;
                    return !Equals(a.Parameter.Register, a.Register);
                }).ToList();
            }
            var twisted = Twisted();
            while (twisted.Count > 1) {
                var parameterAssignments = twisted.Where(parameterAssignment => !twisted.Any(a => a != parameterAssignment && Equals(a.Register, parameterAssignment.Parameter.Register))).ToList();
                if (!parameterAssignments.Any()) break;
                foreach (var parameterAssignment in parameterAssignments) {
                    parameterAssignment.Close(this);
                }
                twisted = Twisted();
            }
            foreach (var assignment in ParameterAssignments.Where(assignment => assignment.Done)) {
                assignment.Close(this);
            }
        }

        private bool IsSourceVariable(Register register)
        {
            foreach (var parameterAssignment in ParameterAssignments) {
                if (parameterAssignment.Done || Equals(parameterAssignment.Parameter.Register, register)) continue;
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
            switch (register) {
                case ByteRegister byteRegister:
                    byteRegister.Load(this, operand);
                    break;
                case WordRegister wordRegister:
                    wordRegister.Load(this, operand);
                    break;
            }

            if (!Equals(operand.Register, register)) {
                ChangedRegisters.Add(register);
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
                        ByteOperation.UsingAnyRegister(this, candidates, null, operand, register =>
                        {
                            register.Load(this, operand);
                            register.StoreToMemory(this, label);
                        });
                    }
                }
                else {
                    StoreWord(operand, label);
                }
                assignment.SetDone(this, null);
            }
        }

        protected virtual void StoreWord(Operand operand, string label)
        {
            WordOperation.UsingAnyRegister(this, register =>
            {
                register.Load(this, operand);
                register.StoreToMemory(this, label);
            });
        }


        protected void StoreParametersViaPointer()
        {
            WordOperation.UsingAnyRegister(this, WordOperation.PointerRegisters(0), pointerRegister =>
            {
                var index = 0;
                var count = ParameterAssignments.Count(a => a.Parameter.Register == null);
                foreach (var assignment in ParameterAssignments.Where(assignment => assignment.Parameter.Register == null)) {
                    Debug.Assert(!assignment.Done);
                    var parameter = assignment.Parameter;
                    var last = index >= count - 1;

                    void StoreByte(ByteRegister register)
                    {
                        register.StoreIndirect(this, pointerRegister, 0);
                        if (!last) {
                            pointerRegister.Add(this, 1);
                        }
                    }

                    void StoreWordViaRegister(WordRegister register)
                    {
                        Debug.Assert(register.Low != null);
                        Debug.Assert(register.High != null);
                        register.Low.StoreIndirect(this, pointerRegister, 0);
                        pointerRegister.Add(this, 1);
                        register.High.StoreIndirect(this, pointerRegister, 0);
                        if (!last) {
                            pointerRegister.Add(this, 1);
                        }
                    }

                    var operand = assignment.Operand;
                    if (index == 0) {
                        pointerRegister.LoadConstant(this, TargetFunction.ParameterLabel(parameter));
                    }

                    if (parameter.Type.ByteCount == 1) {
                        switch (operand) {
                            case IntegerOperand integerOperand:
                                ByteOperation.StoreConstantIndirect(this, pointerRegister, 0,
                                    integerOperand.IntegerValue);
                                if (!last) {
                                    pointerRegister.Add(this, 1);
                                }
                                break;
                            case VariableOperand variableOperand when variableOperand.Variable.Register is ByteRegister register: {
                                    Debug.Assert(variableOperand.Offset == 0);
                                    StoreByte(register);
                                    break;
                                }
                            case VariableOperand _:
                                ByteOperation.UsingAnyRegister(this, ByteOperation.Accumulators, register =>
                                {
                                    register.Load(this, operand);
                                    StoreByte(register);
                                });
                                break;
                            default:
                                ByteOperation.UsingAnyRegister(this, register =>
                                {
                                    register.Load(this, operand);
                                    StoreByte(register);
                                });
                                break;
                        }
                    }
                    else {
                        if (operand.Register is WordRegister wordRegister && wordRegister.IsPair()) {
                            wordRegister.Load(this, operand);
                            StoreWordViaRegister(wordRegister);
                        }
                        else {
                            WordOperation.UsingAnyRegister(this, WordOperation.PairRegisters, register =>
                            {
                                register.Load(this, operand);
                                StoreWordViaRegister(register);
                            });
                        }
                    }
                    assignment.SetDone(this, null);
                    ++index;
                }
            });
        }
    }
}